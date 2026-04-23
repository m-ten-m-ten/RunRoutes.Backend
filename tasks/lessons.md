# 学びの記録

## 2026-04-23 — タグ管理機能の実装

### EF Core の `xmin` 並行性トークンはマイグレーションで AddColumn が出る

Npgsql の慣用的な方法として `Tag.Version` プロパティを `.HasColumnName("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken()` でマップしたが、`dotnet ef migrations add` が `AddColumn<uint>("xmin", ...)` を生成してしまった。`xmin` は PostgreSQL がすべての行に自動で持つシステム列なので、DDL で追加してはいけない (追加すると衝突する)。

**対応**: マイグレーションの `Up`/`Down` を手動で no-op に書き換えた。スナップショット側には `xmin` がプロパティとして残るため、以降のマイグレーションで再度 `AddColumn` が走ることはない。

備考: `UseXminAsConcurrencyToken()` ヘルパーも内部的には shadow property を同様にマップするだけで、同じ症状になる。クライアントに `RowVersion` を往復させる要件がある場合は、明示プロパティ + Up/Down を空にする、の流れを踏襲する。

### InMemory プロバイダは Concurrency Token を強制しない

`Microsoft.EntityFrameworkCore.InMemory` は `IsConcurrencyToken()` や `xmin`/`xid` の挙動を再現しない (常に成功する)。そのため、統合テストで「古い `RowVersion` で送ると 409」を検証することはできない。

**方針**: 並行性違反のサーバ挙動は **単体テスト側** で `Mock<ITagRepository>` に `UpdateWithConcurrencyCheckAsync` が `ConflictException` を投げるようセットアップして検証し、統合テストは正常系 (RowVersion を往復させて更新・削除ができる) のみをカバーする。PG で実地検証したい場合は Testcontainers の導入を検討する (今回はスコープ外)。

### InMemory DB 名を共有するとテストクラス間で状態が漏れる

`WebApplicationFactory` の `options.UseInMemoryDatabase("TestDb")` のように定数名で DB を共有すると、`IClassFixture` ごとの factory インスタンスは別でも **同じプロセス内では同じ InMemory DB インスタンス** を指す。結果として `AuthIntegrationTests` が残したユーザーが `TagsAdminIntegrationTests` にも見えてしまう。

**対応**: `TestWebApplicationFactory` に `DatabaseName = $"TestDb_{Guid.NewGuid():N}"` を持たせ、`UseInMemoryDatabase(DatabaseName)` で参照することで、factory インスタンスごとに独立した DB を割り当てた。テストクラス内での共有は従来通り (IClassFixture で 1 インスタンス) で、各テストメソッドのコンストラクタで DB 全削除 → 再シードすることで順序独立性を担保。

### `[Authorize(Roles = "Admin")]` は `AddAuthorization()` のままで機能する

カスタムポリシーを組む前に、ASP.NET Core 標準ではロールクレーム (`ClaimTypes.Role`) をトークンに入れるだけで `[Authorize(Roles = "Admin")]` が機能する。`AddAuthorizationBuilder()` や `AddPolicy(...)` を追加する必要はない。ポリシーベースの認可が必要になる (例: 「Admin または 所有者」) までは、ロール属性のままで十分。

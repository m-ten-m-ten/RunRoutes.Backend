# 管理者によるタグ管理機能 実装タスク

プラン: `C:\Users\mtenm\.claude\plans\wild-singing-finch.md`

## 1. ロール基盤

- [x] `UserRole` enum 作成 (`src/RunRoutes.Core/Entities/UserRole.cs`)
- [x] `User.Role` プロパティ追加
- [x] `UserConfiguration` に `Role` マッピング
- [x] EF マイグレーション `AddUserRole` 作成

## 2. JWT・認証統合

- [x] `JwtService.GenerateAccessToken` に `ClaimTypes.Role` クレーム追加
- [x] `AuthService.RegisterAsync` で `Role = UserRole.User` を明示セット
- [x] `MeResponse` を `(UserDto User, string Role)` に拡張
- [x] `AuthService.GetMeAsync` で `Role` を返却

## 3. 管理者シーダー

- [x] `AdminSettings` 追加
- [x] `AdminRoleSeeder` 実装 (メール一致で Admin 昇格、ログ出力)
- [x] `Program.cs` で `Configure<AdminSettings>` バインド + 起動時 `RunAsync`
- [x] `appsettings.json` に `"Admin": { "AdminEmails": [] }` 追加

## 4. タグ Concurrency Token

- [x] `Tag.Version` (uint) プロパティ追加
- [x] `TagConfiguration` に `xmin` マッピング (`IsConcurrencyToken`)
- [x] EF マイグレーション `AddTagConcurrencyToken` 作成 (Up/Down は no-op、xmin はシステム列)

## 5. タグ CRUD

- [x] Tag DTO 追加 (`src/RunRoutes.Core/DTOs/Tags/TagDtos.cs`)
- [x] `ITagRepository` 拡張
- [x] `TagRepository` 実装 (`DbUpdateConcurrencyException` → `ConflictException`)
- [x] `ITagService` / `TagService` 実装
- [x] `TagsController` に POST/PUT/DELETE (`[Authorize(Roles="Admin")]`) 追加、`GetAll` を `TagSummaryDto` に切替
- [x] DI 登録 (`ITagService`, `AdminRoleSeeder`)

## 6. テスト

- [x] `TagServiceTests` 単体 (15 ケース)
- [x] `TagsAdminIntegrationTests` 統合 (11 ケース: 401/403/201/400/409/200/204/404/使用中 409/未認証 GET)
- [x] `AdminRoleSeederTests` 統合 (5 ケース)
- [x] `TestWebApplicationFactory` を GUID 付き InMemory DB 名に変更しテストクラス間を分離

## 7. 検証

- [x] `dotnet build RunRoutes.sln` — 警告 0 / エラー 0
- [x] `dotnet test` — 56 / 56 パス (Core 37、Api 19)
- [x] `dotnet ef migrations has-pending-model-changes` — 差分なし
- [x] レビュー欄記入 / `lessons.md` 更新

---

## レビュー

### 主な変更点
- **管理者ロール** を `UserRole` enum で新設し、マイグレーション `AddUserRole` で `users.role` (int, default 0) を追加。
- **JWT に `ClaimTypes.Role`** を含めることで `[Authorize(Roles = "Admin")]` をそのまま利用可能 (`AddAuthorizationBuilder()` 等は不要)。
- **初期管理者** は `appsettings.Admin.AdminEmails` に列挙したメールを起動時に `AdminRoleSeeder` が昇格。新規ユーザー自動作成はせず、既存ユーザーの昇格のみに限定。
- **タグ CRUD** は `[Authorize(Roles = "Admin")]` で保護。削除ポリシーは `HasCoursesAsync` で使用中チェック → 409。名前は 50 文字以内 + 前後空白トリム + UNIQUE 重複時 409。
- **楽観的並行性制御** は PostgreSQL の `xmin` システム列を concurrency token として利用 (`Tag.Version` → `HasColumnName("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken()`)。クライアントは `TagSummaryDto.RowVersion` を UPDATE/DELETE 時に往復。並行性違反は `DbUpdateConcurrencyException` を `ConflictException` に変換して 409。
- **テスト分離**: `TestWebApplicationFactory` の InMemory DB 名を GUID 化し、テストクラス間の状態漏れを防止。タグ統合テストはコンストラクタで毎回 DB を全削除 → 管理者/一般ユーザーを再シード。

### 注意点
- `xmin` は PG システム列のため `AddColumn` は発行せず、マイグレーションの Up/Down は no-op のまま残している。EF のモデルスナップショットには含まれるので、以降の `dotnet ef migrations add` で再度 `xmin` を追加しようとすることはない。
- InMemory プロバイダは `xmin`/`IsConcurrencyToken` を強制しないため、並行性違反の失敗パスは **単体テストのみ** でカバー (リポジトリのモックで `ConflictException` を発生)。本番 PG での実地動作確認は手動テストで実施する必要がある。

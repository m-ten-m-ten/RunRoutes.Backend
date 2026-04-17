# RunRoutes Backend - プロジェクト固有ルール

共通のワークフロー・原則・テスト戦略は `~/.claude/CLAUDE.md` を参照すること。
このファイルには当プロジェクトでの運用ルールと具体化のみを記載する。

---

## タスク管理

1. **まず計画を立てる**：チェック可能な項目として `tasks/todo.md` に計画を書く
2. **計画を確認する**：実装を開始する前に確認する
3. **進捗を記録する**：完了した項目を随時マークしていく
4. **変更を説明する**：各ステップで高レベルのサマリーを提供する
5. **結果をドキュメント化する**：`tasks/todo.md` にレビューセクションを追加する
6. **学びを記録する**：修正を受けた後に `tasks/lessons.md` を更新する（共通ルールの「自己改善ループ」の記録先）

---

## ビルド・検証コマンド

共通ルール「完了前に必ず検証する」の具体化。

- **ビルド確認**：実装後は必ず `dotnet build RunRoutes.sln` を実行してビルドが通ることを確認する
- **テスト実行**：`dotnet test` が全てパスすることをタスク完了条件とする

---

## テスト戦略の具体化

共通テスト戦略（`~/.claude/CLAUDE.md`）を当プロジェクトでは次のように運用する。

### レイヤー区分
- **基礎レイヤー（実装と同時にテストを書く）**
  - `src/RunRoutes.Core/Services/` — ビジネスロジック（依存はインターフェースでモック可能）
  - `src/RunRoutes.Core/Helpers/` — ユーティリティ・純粋関数
  - `src/RunRoutes.Core/Entities/`・`src/RunRoutes.Core/DTOs/` — 振る舞いを持つもののみ
- **上位レイヤー（機能単位でまとめてテストを書く）**
  - `src/RunRoutes.Api/Controllers/` — エンドポイント
  - `src/RunRoutes.Api/Middleware/`・`src/RunRoutes.Api/Extensions/`
  - `src/RunRoutes.Infrastructure/` — EF Core リポジトリ・外部サービス実装（統合テストで検証）

### テスト環境
- **コマンド**：`dotnet test`（単一プロジェクトは `dotnet test tests/RunRoutes.Core.Tests` のように指定）
- **フレームワーク**：
  - 単体テスト：xUnit + Moq（`tests/RunRoutes.Core.Tests`）
  - 統合テスト：xUnit + `WebApplicationFactory<Program>` + EF Core InMemory（`tests/RunRoutes.Api.Tests`）
- **配置ルール**：
  - 単体テスト → `tests/RunRoutes.Core.Tests/<ClassName>Tests.cs`
  - 統合テスト → `tests/RunRoutes.Api.Tests/<Feature>IntegrationTests.cs`
  - 既存パターン（[AuthServiceTests.cs](tests/RunRoutes.Core.Tests/AuthServiceTests.cs)、[AuthIntegrationTests.cs](tests/RunRoutes.Api.Tests/AuthIntegrationTests.cs)）に倣う

### モック方針
- 単体テストでは依存インターフェース（`IUserRepository`、`IJwtService`、`IEmailService` 等）を `Moq` で差し替える
- 統合テストでは [TestWebApplicationFactory.cs](tests/RunRoutes.Api.Tests/TestWebApplicationFactory.cs) を再利用し、DB は `Microsoft.EntityFrameworkCore.InMemory`、メール送信は no-op サービスに差し替える
- 各テストは独立実行可能にする（テスト間で共有状態に依存しない／`InMemory` DB はテストごとに分離する）
- 新規に外部依存を追加する場合はインターフェースを切り、DI 経由で差し替えられるようにする

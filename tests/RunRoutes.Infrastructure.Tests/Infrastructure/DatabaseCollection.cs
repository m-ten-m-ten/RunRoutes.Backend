namespace RunRoutes.Infrastructure.Tests.Infrastructure;

[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<PostgresContainerFixture>
{
    // このクラスは中身が空でOK。
    // [CollectionDefinition] 属性と ICollectionFixture<T> インターフェースだけが意味を持つ。
    // xUnit のフレームワークがこの宣言を見て Collection を構築する。
}
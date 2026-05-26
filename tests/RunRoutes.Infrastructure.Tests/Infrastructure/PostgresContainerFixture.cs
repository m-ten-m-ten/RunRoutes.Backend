using Microsoft.EntityFrameworkCore;
using Npgsql;
using Respawn;
using RunRoutes.Infrastructure.Data;
using Testcontainers.PostgreSql;

namespace RunRoutes.Infrastructure.Tests.Infrastructure;

public class PostgresContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgis/postgis:17-3.3")
        .WithDatabase("runroutes_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    private Respawner _respawner = null!;

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        // 1. コンテナ起動
        await _container.StartAsync();

        // 2. マイグレーション適用
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString, npgsql => npgsql.UseNetTopologySuite())
            .Options;

        await using var db = new AppDbContext(options);
        await db.Database.MigrateAsync();

        // 3. Respawn のセットアップ
        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();

        _respawner = await Respawner.CreateAsync(conn, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
            TablesToIgnore = [new Respawn.Graph.Table("__EFMigrationsHistory")],
        });
    }

    public async Task ResetAsync()
    {
        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();
        await _respawner.ResetAsync(conn);
    }

    public AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString, npgsql => npgsql.UseNetTopologySuite())
            .Options;
        return new AppDbContext(options);
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
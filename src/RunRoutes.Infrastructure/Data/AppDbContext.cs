using Microsoft.EntityFrameworkCore;
using RunRoutes.Core.Audit;
using RunRoutes.Core.Common;
using RunRoutes.Core.Common.DomainEvents;
using RunRoutes.Core.Courses;
using RunRoutes.Core.Sessions;
using RunRoutes.Core.Tags;
using RunRoutes.Core.Users;

namespace RunRoutes.Infrastructure.Data;

public class AppDbContext(
    DbContextOptions<AppDbContext> options,
    IDomainEventDispatcher? dispatcher = null) : DbContext(options)
{
    private readonly IDomainEventDispatcher? _dispatcher = dispatcher;

    public DbSet<User> Users => Set<User>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<AuditLogEntry> AuditLogs => Set<AuditLogEntry>();
    public DbSet<Session> Sessions => Set<Session>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("extensions", "postgis");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // 1. 追跡中エンティティから DomainEvents を収集
        var aggregates = ChangeTracker.Entries<AggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        var events = aggregates.SelectMany(a => a.DomainEvents).ToList();

        // 2. ハンドラを実行(まだ SaveChanges していないので、ハンドラの DbContext 変更は次の SaveChanges で一緒にコミットされる)
        if (_dispatcher is not null && events.Count > 0)
        {
            await _dispatcher.DispatchAsync(events, cancellationToken);
        }

        // 3. ドメインイベントをクリア
        foreach (var aggregate in aggregates)
        {
            aggregate.ClearDomainEvents();
        }

        // 4. 業務操作 + ハンドラが追加した監査ログを一括コミット
        return await base.SaveChangesAsync(cancellationToken);
    }
}

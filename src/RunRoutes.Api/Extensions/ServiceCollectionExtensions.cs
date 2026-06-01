using RunRoutes.Core.Common;
using RunRoutes.Core.Common.Commands;
using RunRoutes.Core.Common.DomainEvents;
using RunRoutes.Core.Common.Queries;
using RunRoutes.Core.Courses;
using RunRoutes.Core.Sessions;
using RunRoutes.Core.Tags;
using RunRoutes.Core.Users;
using RunRoutes.Infrastructure.Auth;
using RunRoutes.Infrastructure.Commands;
using RunRoutes.Infrastructure.DomainEvents;
using RunRoutes.Infrastructure.Queries;
using RunRoutes.Infrastructure.Repositories;
using RunRoutes.Infrastructure.Services;

namespace RunRoutes.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICourseRepository, CourseRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<ISessionRepository, SessionRepository>();

        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<ITagService, TagService>();
        services.AddScoped<AdminRoleSeeder>();

        // ドメインイベント基盤
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        // CQRS基盤
        services.AddScoped<ICommandDispatcher, CommandDispatcher>();
        services.AddScoped<IQueryDispatcher, QueryDispatcher>();

        // ICurrentUserService の実装は Api 層にある(HTTP 依存のため)
        services.AddScoped<ICurrentUserService, Services.CurrentUserService>();

        // ドメインイベントハンドラを Infrastructure アセンブリからスキャン登録
        services.Scan(scan => scan
            .FromAssemblyOf<DomainEventDispatcher>()
            .AddClasses(c => c.AssignableTo(typeof(IDomainEventHandler<>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        // Command ハンドラを Infrastructure アセンブリからスキャン登録
        services.Scan(scan => scan
            .FromAssemblyOf<CommandDispatcher>()
            .AddClasses(c => c.AssignableTo(typeof(ICommandHandler<,>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        // Query ハンドラを Infrastructure アセンブリからスキャン登録
        services.Scan(scan => scan
            .FromAssemblyOf<QueryDispatcher>()
            .AddClasses(c => c.AssignableTo(typeof(IQueryHandler<,>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        return services;
    }
}

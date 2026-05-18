using RunRoutes.Core.Common;
using RunRoutes.Core.Common.DomainEvents;
using RunRoutes.Core.Courses;
using RunRoutes.Core.Tags;
using RunRoutes.Core.Users;
using RunRoutes.Infrastructure.Auth;
using RunRoutes.Infrastructure.DomainEvents;
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

        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICourseService, CourseService>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<ITagService, TagService>();
        services.AddScoped<AdminRoleSeeder>();

        // ドメインイベント基盤
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        // ICurrentUserService の実装は Api 層にある(HTTP 依存のため)
        services.AddScoped<ICurrentUserService, Services.CurrentUserService>();

        // ドメインイベントハンドラを Infrastructure アセンブリからスキャン登録
        services.Scan(scan => scan
            .FromAssemblyOf<DomainEventDispatcher>()
            .AddClasses(c => c.AssignableTo(typeof(IDomainEventHandler<>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        return services;
    }
}

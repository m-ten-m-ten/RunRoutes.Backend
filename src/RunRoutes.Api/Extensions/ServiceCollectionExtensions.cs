using RunRoutes.Core.Interfaces.Repositories;
using RunRoutes.Core.Interfaces.Services;
using RunRoutes.Core.Services;
using RunRoutes.Infrastructure.Repositories;
using RunRoutes.Infrastructure.Services;

namespace RunRoutes.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICourseRepository, CourseRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<ITagRepository, TagRepository>();

        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICourseService, CourseService>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<ITagService, TagService>();
        services.AddScoped<AdminRoleSeeder>();

        return services;
    }
}

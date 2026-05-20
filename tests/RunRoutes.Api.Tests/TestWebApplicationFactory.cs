using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunRoutes.Core.Users;
using RunRoutes.Infrastructure.Data;

namespace RunRoutes.Api.Tests;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    public string DatabaseName { get; } = $"TestDb_{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // AppDbContext 関連の登録を全て削除する（Npgsql 設定も含む）
            var descriptors = services
                .Where(d =>
                    d.ServiceType == typeof(AppDbContext) ||
                    d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                    d.ServiceType == typeof(DbContextOptions) ||
                    (d.ServiceType.IsGenericType &&
                     d.ServiceType.GetGenericArguments().Contains(typeof(AppDbContext))))
                .ToList();
            foreach (var d in descriptors) services.Remove(d);

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(DatabaseName));

            // SMTP 呼び出しを避けるため EmailService を no-op に差し替える
            var emailDescriptor = services
                .FirstOrDefault(d => d.ServiceType == typeof(IEmailService));
            if (emailDescriptor != null) services.Remove(emailDescriptor);
            services.AddScoped<IEmailService, NoOpEmailService>();
        });
    }
}

internal class NoOpEmailService : IEmailService
{
    public Task SendActivationEmailAsync(string email, string token) => Task.CompletedTask;
    public Task SendEmailChangeEmailAsync(string email, string token) => Task.CompletedTask;
}

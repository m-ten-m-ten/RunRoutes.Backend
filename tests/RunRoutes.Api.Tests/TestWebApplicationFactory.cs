using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunRoutes.Core.Interfaces.Services;
using RunRoutes.Infrastructure.Data;

namespace RunRoutes.Api.Tests;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove ALL AppDbContext-related registrations (including Npgsql configuration)
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
                options.UseInMemoryDatabase("TestDb"));

            // Replace EmailService with no-op to avoid SMTP calls
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

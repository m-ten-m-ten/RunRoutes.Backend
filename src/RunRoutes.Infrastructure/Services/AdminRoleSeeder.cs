using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RunRoutes.Core.Settings;
using RunRoutes.Core.Users;
using RunRoutes.Infrastructure.Data;

namespace RunRoutes.Infrastructure.Services;

public class AdminRoleSeeder(
    AppDbContext db,
    IOptions<AdminSettings> settings,
    ILogger<AdminRoleSeeder> logger)
{
    private readonly AdminSettings _settings = settings.Value;

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        if (_settings.AdminEmails.Count == 0)
        {
            logger.LogDebug("AdminEmails が空のためシードをスキップします");
            return;
        }

        var normalizedEmails = _settings.AdminEmails
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(e => EmailAddress.Create(e.Trim().ToLowerInvariant()))
            .ToList();

        if (normalizedEmails.Count == 0) return;

        var users = await db.Users
            .Where(u => normalizedEmails.Contains(u.Email))
            .ToListAsync(cancellationToken);

        foreach (var email in normalizedEmails)
        {
            if (!users.Any(u => u.Email == email))
            {
                logger.LogWarning(
                    "AdminEmails に設定されたメール {Email} に対応するユーザーが存在しません。管理者昇格をスキップします",
                    email);
            }
        }

        var promoted = 0;
        foreach (var user in users)
        {
            if (user.Role == UserRole.Admin) continue;
            user.PromoteToAdmin(DateTime.UtcNow);
            promoted++;
        }

        if (promoted > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("{Count} 名のユーザーを Admin ロールに昇格しました", promoted);
        }
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunRoutes.Core.Courses;
using RunRoutes.Core.Sessions;
using RunRoutes.Core.Users;
using RunRoutes.Core.Users.Dtos;
using RunRoutes.Core.Users.Events;
using RunRoutes.Infrastructure.Data;

namespace RunRoutes.Api.Tests;

public class UserDeletionIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private const string UserEmail = "deletion@example.com";
    private const string UserName = "deletionuser";
    private const string Password = "Password123!";

    public UserDeletionIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        ResetAndSeed();
    }

    private void ResetAndSeed()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.AuditLogs.RemoveRange(db.AuditLogs);
        db.Sessions.RemoveRange(db.Sessions);
        db.Comments.RemoveRange(db.Comments);
        db.Courses.RemoveRange(db.Courses);
        db.Tags.RemoveRange(db.Tags);
        db.Users.RemoveRange(db.Users);
        db.SaveChanges();

        db.Users.Add(TestUserBuilder.CreateActivated(UserEmail, UserName, Password));
        db.SaveChanges();
    }

    private async Task<string> LoginAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { Email = UserEmail, Password });
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<LoginResult>();
        return result!.AccessToken;
    }

    private async Task<(HttpClient client, Guid userId)> CreateAuthenticatedClientAsync()
    {
        var client = _factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userId = db.Users.First(u => u.Email.Value == UserEmail).Id;
        return (client, userId);
    }

    [Fact]
    public async Task DELETE_認証なしで401()
    {
        var client = _factory.CreateClient();

        var response = await client.DeleteAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DELETE_正常にユーザーが削除される()
    {
        var (client, userId) = await CreateAuthenticatedClientAsync();

        var response = await client.DeleteAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<DeleteAccountResponse>();
        Assert.Equal("アカウントを削除しました", body!.Message);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.FindAsync(userId);
        Assert.Null(user);
    }

    [Fact]
    public async Task DELETE_監査ログが正しく記録される()
    {
        var (client, userId) = await CreateAuthenticatedClientAsync();

        var response = await client.DeleteAsync("/api/auth/me");
        response.EnsureSuccessStatusCode();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var log = await db.AuditLogs
            .Where(a => a.TargetId == userId && a.EventType == nameof(UserRemovedEvent))
            .SingleOrDefaultAsync();

        Assert.NotNull(log);
        Assert.Equal("User", log.TargetType);
        Assert.Equal(userId, log.ActorId);
        Assert.Contains(userId.ToString(), log.Payload);
    }

    [Fact]
    public async Task DELETE_refreshTokenCookieが削除される()
    {
        var (client, _) = await CreateAuthenticatedClientAsync();

        var response = await client.DeleteAsync("/api/auth/me");
        response.EnsureSuccessStatusCode();

        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var setCookies));
        var refreshCookie = setCookies!.FirstOrDefault(c =>
            c.StartsWith("refreshToken=", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(refreshCookie);
        Assert.True(
            refreshCookie!.Contains("expires=", StringComparison.OrdinalIgnoreCase) ||
            refreshCookie.Contains("max-age=0", StringComparison.OrdinalIgnoreCase),
            $"refreshToken Cookie に削除指示が含まれていません: {refreshCookie}");
    }

    [Fact]
    public async Task DELETE_ユーザー削除と監査ログは同一コミットで記録される()
    {
        var (client, userId) = await CreateAuthenticatedClientAsync();

        var response = await client.DeleteAsync("/api/auth/me");
        response.EnsureSuccessStatusCode();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.FindAsync(userId);
        var log = await db.AuditLogs
            .Where(a => a.TargetId == userId && a.EventType == nameof(UserRemovedEvent))
            .SingleOrDefaultAsync();

        Assert.Null(user);
        Assert.NotNull(log);
    }

    [Fact]
    public void Session_Course_CommentのUser外部キーはCascade削除が設定されている()
    {
        // InMemory プロバイダーは未トラックの dependent をカスケードしないため
        // 実 DB (PostgreSQL) で機能する Cascade 設定を EF Model のメタデータで検証する
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        AssertUserFkIsCascade<Session>(db);
        AssertUserFkIsCascade<Course>(db);
        AssertUserFkIsCascade<Comment>(db);
    }

    private static void AssertUserFkIsCascade<TDependent>(AppDbContext db) where TDependent : class
    {
        var entityType = db.Model.FindEntityType(typeof(TDependent))
            ?? throw new InvalidOperationException($"{typeof(TDependent).Name} が EF モデルに登録されていません");
        var userFk = entityType.GetForeignKeys()
            .SingleOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(User))
            ?? throw new InvalidOperationException($"{typeof(TDependent).Name} に User への外部キーがありません");
        Assert.Equal(DeleteBehavior.Cascade, userFk.DeleteBehavior);
    }

    private record LoginResult(string AccessToken);
}

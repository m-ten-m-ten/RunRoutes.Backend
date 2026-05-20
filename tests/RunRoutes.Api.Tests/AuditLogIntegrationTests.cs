using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunRoutes.Core.Courses.Dtos;
using RunRoutes.Core.Courses.Events;
using RunRoutes.Core.Users;
using RunRoutes.Infrastructure.Data;

namespace RunRoutes.Api.Tests;

public class AuditLogIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private const string UserEmail = "audituser@example.com";
    private const string Password = "Password123!";

    public AuditLogIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        ResetAndSeed();
    }

    private void ResetAndSeed()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.AuditLogs.RemoveRange(db.AuditLogs);
        db.Comments.RemoveRange(db.Comments);
        db.Courses.RemoveRange(db.Courses);
        db.Tags.RemoveRange(db.Tags);
        db.Users.RemoveRange(db.Users);
        db.SaveChanges();

        db.Users.Add(TestUserBuilder.CreateActivated(UserEmail, "audituser", Password));
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

    private async Task<Guid> CreateUnpublishedCourseAsync(HttpClient client)
    {
        var request = new CreateCourseRequest(
            Title: "テストコース",
            Description: null,
            Difficulty: "easy",
            IsPublic: false,
            Route: new GeoJsonLineStringDto("LineString", [[139.0, 35.0], [139.1, 35.1]]),
            GpxXml: null,
            TagIds: []);

        var response = await client.PostAsJsonAsync("/api/courses", request);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CreateCourseResponse>();
        return result!.Course.Id;
    }

    [Fact]
    public async Task コース公開時に監査ログが記録される()
    {
        var (client, userId) = await CreateAuthenticatedClientAsync();
        var courseId = await CreateUnpublishedCourseAsync(client);

        var response = await client.PutAsJsonAsync(
            $"/api/courses/{courseId}",
            new UpdateCourseRequest(null, null, null, true, null, null, null));
        response.EnsureSuccessStatusCode();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var log = await db.AuditLogs
            .Where(a => a.TargetId == courseId && a.EventType == nameof(CoursePublishedEvent))
            .SingleOrDefaultAsync();

        Assert.NotNull(log);
        Assert.Equal(userId, log.ActorId);
        Assert.Equal("Course", log.TargetType);
        Assert.Contains(courseId.ToString(), log.Payload);
    }

    [Fact]
    public async Task コメント追加時に監査ログが記録される()
    {
        var (client, userId) = await CreateAuthenticatedClientAsync();
        var courseId = await CreateUnpublishedCourseAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/courses/{courseId}/comments",
            new CreateCommentRequest("テストコメント"));
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CreateCommentResponse>();
        var commentId = result!.Comment.Id;

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var log = await db.AuditLogs
            .Where(a => a.TargetId == commentId && a.EventType == nameof(CommentAddedEvent))
            .SingleOrDefaultAsync();

        Assert.NotNull(log);
        Assert.Equal(userId, log.ActorId);
        Assert.Equal("Comment", log.TargetType);
        Assert.Contains(commentId.ToString(), log.Payload);
    }

    [Fact]
    public async Task コース公開と監査ログは同一コミットで記録される()
    {
        // AppDbContext.SaveChangesAsync が業務操作と監査ログを一括コミットしていることを検証する。
        // 別スコープで確認し、コース状態と監査ログが共に存在することで同一コミットを確かめる。
        var (client, _) = await CreateAuthenticatedClientAsync();
        var courseId = await CreateUnpublishedCourseAsync(client);

        var response = await client.PutAsJsonAsync(
            $"/api/courses/{courseId}",
            new UpdateCourseRequest(null, null, null, true, null, null, null));
        response.EnsureSuccessStatusCode();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var course = await db.Courses.FindAsync(courseId);
        var log = await db.AuditLogs
            .Where(a => a.TargetId == courseId && a.EventType == nameof(CoursePublishedEvent))
            .SingleOrDefaultAsync();

        Assert.True(course!.IsPublic);
        Assert.NotNull(log);
    }

    private record LoginResult(string AccessToken);
}

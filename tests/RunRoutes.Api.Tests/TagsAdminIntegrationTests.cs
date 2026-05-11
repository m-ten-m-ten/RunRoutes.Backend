using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using NetTopologySuite.Geometries;
using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Courses;
using RunRoutes.Core.Tags;
using RunRoutes.Core.Tags.Dtos;
using RunRoutes.Core.Users;
using RunRoutes.Infrastructure.Data;

namespace RunRoutes.Api.Tests;

// NOTE: 並行性エラーのパス（RowVersion が古い場合）は TagServiceTests で検証している。
// InMemory プロバイダは xmin / IsConcurrencyToken を強制しないため、
// このシナリオは統合テスト層では再現できない。
public class TagsAdminIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private const string AdminEmail = "tagadmin@example.com";
    private const string UserEmail = "taguser@example.com";
    private const string Password = "Password123!";

    public TagsAdminIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        ResetAndSeed();
    }

    private void ResetAndSeed()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Comments.RemoveRange(db.Comments);
        db.Courses.RemoveRange(db.Courses);
        db.Tags.RemoveRange(db.Tags);
        db.Users.RemoveRange(db.Users);
        db.SaveChanges();

        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = AdminEmail,
            Username = "tagadmin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password),
            IsActive = true,
            Role = UserRole.Admin,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = UserEmail,
            Username = "taguser",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password),
            IsActive = true,
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        db.SaveChanges();
    }

    private async Task<string> LoginAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password });
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<LoginResult>();
        return result!.AccessToken;
    }

    private async Task<HttpClient> CreateClientForAsync(string email)
    {
        var client = _factory.CreateClient();
        var token = await LoginAsync(client, email);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task Post_未認証で401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/tags", new CreateTagRequest("trail"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_一般ユーザーで403()
    {
        var client = await CreateClientForAsync(UserEmail);

        var response = await client.PostAsJsonAsync("/api/tags", new CreateTagRequest("trail"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Post_管理者で201()
    {
        var client = await CreateClientForAsync(AdminEmail);

        var response = await client.PostAsJsonAsync("/api/tags", new CreateTagRequest("trail"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CreateTagResponse>();
        Assert.NotNull(body);
        Assert.Equal("trail", body!.Tag.Name);
        Assert.NotEqual(Guid.Empty, body.Tag.Id);
    }

    [Fact]
    public async Task Post_重複名で409()
    {
        var client = await CreateClientForAsync(AdminEmail);
        await client.PostAsJsonAsync("/api/tags", new CreateTagRequest("duplicate"));

        var response = await client.PostAsJsonAsync("/api/tags", new CreateTagRequest("duplicate"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal(ErrorCodes.TagNameDuplicate, error!.Code);
    }

    [Fact]
    public async Task Post_空白名で400()
    {
        var client = await CreateClientForAsync(AdminEmail);

        var response = await client.PostAsJsonAsync("/api/tags", new CreateTagRequest("   "));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_対象なしで404()
    {
        var client = await CreateClientForAsync(AdminEmail);

        var response = await client.PutAsJsonAsync(
            $"/api/tags/{Guid.NewGuid()}",
            new UpdateTagRequest("whatever", 0));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_管理者で200()
    {
        var client = await CreateClientForAsync(AdminEmail);
        var created = await client.PostAsJsonAsync("/api/tags", new CreateTagRequest("before"));
        var createdTag = (await created.Content.ReadFromJsonAsync<CreateTagResponse>())!.Tag;

        var response = await client.PutAsJsonAsync(
            $"/api/tags/{createdTag.Id}",
            new UpdateTagRequest("after", createdTag.RowVersion));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<UpdateTagResponse>();
        Assert.Equal("after", body!.Tag.Name);
    }

    [Fact]
    public async Task Delete_管理者で204()
    {
        var client = await CreateClientForAsync(AdminEmail);
        var created = await client.PostAsJsonAsync("/api/tags", new CreateTagRequest("removable"));
        var createdTag = (await created.Content.ReadFromJsonAsync<CreateTagResponse>())!.Tag;

        var response = await client.DeleteAsync(
            $"/api/tags/{createdTag.Id}?rowVersion={createdTag.RowVersion}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getAll = await client.GetAsync("/api/tags");
        var tags = await getAll.Content.ReadFromJsonAsync<List<TagSummaryDto>>();
        Assert.DoesNotContain(tags!, t => t.Id == createdTag.Id);
    }

    [Fact]
    public async Task Delete_存在しないIDで404()
    {
        var client = await CreateClientForAsync(AdminEmail);

        var response = await client.DeleteAsync($"/api/tags/{Guid.NewGuid()}?rowVersion=0");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_使用中タグで409()
    {
        var client = await CreateClientForAsync(AdminEmail);
        var created = await client.PostAsJsonAsync("/api/tags", new CreateTagRequest("inuse"));
        var createdTag = (await created.Content.ReadFromJsonAsync<CreateTagResponse>())!.Tag;

        AttachTagToACourse(createdTag.Id);

        var response = await client.DeleteAsync(
            $"/api/tags/{createdTag.Id}?rowVersion={createdTag.RowVersion}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal(ErrorCodes.TagInUse, error!.Code);
    }

    [Fact]
    public async Task Get_認証なしでも一覧取得できる()
    {
        using (var adminClient = await CreateClientForAsync(AdminEmail))
        {
            await adminClient.PostAsJsonAsync("/api/tags", new CreateTagRequest("public-view"));
        }

        var anonymousClient = _factory.CreateClient();
        var response = await anonymousClient.GetAsync("/api/tags");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tags = await response.Content.ReadFromJsonAsync<List<TagSummaryDto>>();
        Assert.Contains(tags!, t => t.Name == "public-view");
    }

    private void AttachTagToACourse(Guid tagId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var admin = db.Users.First(u => u.Email == AdminEmail);
        var tag = db.Tags.First(t => t.Id == tagId);

        var course = new Course
        {
            Id = Guid.NewGuid(),
            UserId = admin.Id,
            Title = "sample",
            Difficulty = "easy",
            Route = new LineString([new Coordinate(139.0, 35.0), new Coordinate(139.1, 35.1)])
            {
                SRID = 4326,
            },
            DistanceM = 100.0,
            IsPublic = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Tags = [tag],
        };
        db.Courses.Add(course);
        db.SaveChanges();
    }

    private record LoginResult(string AccessToken);
}

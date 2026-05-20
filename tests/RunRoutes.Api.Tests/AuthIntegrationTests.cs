using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using RunRoutes.Core.Users;
using RunRoutes.Infrastructure.Data;

namespace RunRoutes.Api.Tests;

public class AuthIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private const string TestEmail = "integtest@example.com";
    private const string TestPassword = "Password123!";

    public AuthIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        SeedTestUser();
    }

    private void SeedTestUser()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (!db.Users.Any(u => u.Email.Value == TestEmail))
        {
            db.Users.Add(TestUserBuilder.CreateActivated(TestEmail, "integtestuser", TestPassword));
            db.SaveChanges();
        }
    }

    [Fact]
    public async Task 認証なしで保護エンドポイントにアクセスすると401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task 無効なトークンで401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "invalid.token.value");

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task 正常なトークンでアクセスできる()
    {
        var client = _factory.CreateClient();

        // ログインしてアクセストークンを取得する
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = TestEmail,
            Password = TestPassword
        });
        loginResponse.EnsureSuccessStatusCode();

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResult>();
        Assert.NotNull(loginResult?.AccessToken);

        // アクセストークンを使用して保護されたエンドポイントを呼び出す
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginResult.AccessToken);

        var meResponse = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
    }

    private record LoginResult(string AccessToken);
}

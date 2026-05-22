using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RunRoutes.Api.Extensions;
using RunRoutes.Core.Settings;
using RunRoutes.Core.Users;
using RunRoutes.Core.Users.Dtos;

namespace RunRoutes.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService, IOptions<JwtSettings> jwtSettings) : ControllerBase
{
    private readonly JwtSettings _jwtSettings = jwtSettings.Value;

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await authService.RegisterAsync(request);
        return Ok(result);
    }

    [HttpGet("activate")]
    public async Task<IActionResult> Activate([FromQuery] string token)
    {
        await authService.ActivateAsync(token);
        return NoContent();
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var (response, refreshToken) = await authService.LoginAsync(request);
        Response.Cookies.Append("refreshToken", refreshToken, BuildRefreshCookieOptions());
        return Ok(new { response.AccessToken, response.User });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (refreshToken is not null)
            await authService.LogoutAsync(refreshToken);

        Response.Cookies.Delete("refreshToken", new CookieOptions { Path = "/api/auth" });
        return NoContent();
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var refreshToken = Request.Cookies["refreshToken"]
            ?? throw new Core.Common.Exceptions.ValidationException("リフレッシュトークンがありません");

        var (result, newRefreshToken) = await authService.RefreshAsync(refreshToken);
        Response.Cookies.Append("refreshToken", newRefreshToken, BuildRefreshCookieOptions());
        return Ok(result);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMe()
    {
        var userId = User.GetUserId();
        var result = await authService.GetMeAsync(userId);
        return Ok(result);
    }

    [HttpPut("me")]
    [Authorize]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateMeRequest request)
    {
        var userId = User.GetUserId();
        var result = await authService.UpdateMeAsync(userId, request);
        return Ok(result);
    }

    [HttpPut("me/email")]
    [Authorize]
    public async Task<IActionResult> UpdateEmail([FromBody] UpdateEmailRequest request)
    {
        var userId = User.GetUserId();
        var result = await authService.UpdateEmailAsync(userId, request);
        return Ok(result);
    }

    [HttpGet("activate-email")]
    public async Task<IActionResult> ActivateEmail([FromQuery] string token)
    {
        await authService.ActivateEmailAsync(token);
        return NoContent();
    }

    [HttpDelete("me")]
    [Authorize]
    public async Task<IActionResult> RemoveMe()
    {
        var userId = User.GetUserId();
        var result = await authService.RemoveMeAsync(userId);
        Response.Cookies.Delete("refreshToken", new CookieOptions { Path = "/api/auth" });
        return Ok(result);
    }

    private CookieOptions BuildRefreshCookieOptions() => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.None,
        Expires = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
        Path = "/api/auth"
    };
}

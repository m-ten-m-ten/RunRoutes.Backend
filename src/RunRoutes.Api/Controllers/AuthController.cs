using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RunRoutes.Api.Extensions;
using RunRoutes.Core.Auth.Commands.Activate;
using RunRoutes.Core.Auth.Commands.ActivateEmail;
using RunRoutes.Core.Auth.Commands.Login;
using RunRoutes.Core.Auth.Commands.Logout;
using RunRoutes.Core.Auth.Commands.Refresh;
using RunRoutes.Core.Auth.Commands.RegisterUser;
using RunRoutes.Core.Auth.Commands.RemoveMe;
using RunRoutes.Core.Auth.Commands.UpdateEmail;
using RunRoutes.Core.Auth.Commands.UpdateMe;
using RunRoutes.Core.Auth.Queries.GetMe;
using RunRoutes.Core.Common.Commands;
using RunRoutes.Core.Common.Queries;
using RunRoutes.Core.Settings;
using RunRoutes.Core.Users.Dtos;

namespace RunRoutes.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    IQueryDispatcher queryDispatcher,
    ICommandDispatcher commandDispatcher,
    IOptions<JwtSettings> jwtSettings
    ) : ControllerBase
{
    private readonly IQueryDispatcher _queryDispatcher = queryDispatcher;
    private readonly ICommandDispatcher _commandDispatcher = commandDispatcher;
    private readonly JwtSettings _jwtSettings = jwtSettings.Value;

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var command = new RegisterUserCommand(request.Email, request.Username, request.Password);
        var result = await _commandDispatcher.SendAsync(command);
        return Ok(result);
    }

    [HttpGet("activate")]
    public async Task<IActionResult> Activate([FromQuery] string token)
    {
        var command = new ActivateCommand(token);
        await _commandDispatcher.SendAsync(command);
        return NoContent();
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var command = new LoginCommand(request.Email, request.Password);
        var result = await _commandDispatcher.SendAsync(command);
        Response.Cookies.Append("refreshToken", result.RefreshToken, BuildRefreshCookieOptions());
        return Ok(new { result.Response.AccessToken, result.Response.User });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (refreshToken is not null)
        {
            var command = new LogoutCommand(refreshToken);
            await _commandDispatcher.SendAsync(command);
        }

        Response.Cookies.Delete("refreshToken", new CookieOptions { Path = "/api/auth" });
        return NoContent();
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var refreshToken = Request.Cookies["refreshToken"]
            ?? throw new Core.Common.Exceptions.ValidationException("リフレッシュトークンがありません");

        var command = new RefreshCommand(refreshToken);
        var result = await _commandDispatcher.SendAsync(command);
        Response.Cookies.Append("refreshToken", result.NewRefreshToken, BuildRefreshCookieOptions());
        return Ok(result.Response);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMe()
    {
        var userId = User.GetUserId();
        var query = new GetMeQuery(userId);
        var result = await _queryDispatcher.SendAsync(query);
        return Ok(result);
    }

    [HttpPut("me")]
    [Authorize]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateMeRequest request)
    {
        var userId = User.GetUserId();
        var command = new UpdateMeCommand(userId, request.Username, request.CurrentPassword, request.NewPassword);
        var result = await _commandDispatcher.SendAsync(command);
        return Ok(result);
    }

    [HttpPut("me/email")]
    [Authorize]
    public async Task<IActionResult> UpdateEmail([FromBody] UpdateEmailRequest request)
    {
        var userId = User.GetUserId();
        var command = new UpdateEmailCommand(userId, request.NewEmail, request.CurrentPassword);
        var result = await _commandDispatcher.SendAsync(command);
        return Ok(result);
    }

    [HttpGet("activate-email")]
    public async Task<IActionResult> ActivateEmail([FromQuery] string token)
    {
        var command = new ActivateEmailCommand(token);
        await _commandDispatcher.SendAsync(command);
        return NoContent();
    }

    [HttpDelete("me")]
    [Authorize]
    public async Task<IActionResult> RemoveMe()
    {
        var userId = User.GetUserId();
        var command = new RemoveMeCommand(userId);
        var result = await _commandDispatcher.SendAsync(command);
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

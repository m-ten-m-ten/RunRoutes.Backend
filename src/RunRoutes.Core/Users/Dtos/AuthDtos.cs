namespace RunRoutes.Core.Users.Dtos;

public record RegisterRequest(string Email, string Username, string Password);
public record RegisterResponse(string Message);
public record LoginRequest(string Email, string Password);
public record LoginResponse(string AccessToken, UserDto User);
public record RefreshResponse(string AccessToken, UserDto User);
public record MeResponse(UserDto User);
public record UpdateMeRequest(string? Username, string? CurrentPassword, string? NewPassword);
public record UpdateMeResponse(UserDto User);
public record UpdateEmailRequest(string NewEmail, string CurrentPassword);
public record UpdateEmailResponse(string Message);

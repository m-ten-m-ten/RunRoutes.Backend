namespace RunRoutes.Core.Users.Dtos;

public record UserDto(Guid Id, string Email, string Username, string Role, DateTime CreatedAt);

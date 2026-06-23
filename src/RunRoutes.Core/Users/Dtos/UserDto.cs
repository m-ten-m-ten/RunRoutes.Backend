namespace RunRoutes.Core.Users.Dtos;

public record UserDto(Guid Id, string Email, string Username, string Role, DateTime CreatedAt)
{
    public static UserDto FromUser(User user) =>
        new(user.Id, user.Email.Value, user.Username.Value, user.Role.ToString(), user.CreatedAt);
};

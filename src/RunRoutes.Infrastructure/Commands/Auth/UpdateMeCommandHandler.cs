using RunRoutes.Core.Auth.Commands.UpdateMe;
using RunRoutes.Core.Common.Commands;
using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Users;
using RunRoutes.Core.Users.Dtos;

namespace RunRoutes.Infrastructure.Commands.Auth;

public class UpdateMeCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher
) : ICommandHandler<UpdateMeCommand, UpdateMeResponse>
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;

    public async Task<UpdateMeResponse> HandleAsync(
        UpdateMeCommand command,
        CancellationToken cancellationToken
    )
    {
        var user = await _userRepository.GetByIdForUpdateAsync(command.UserId)
            ?? throw new NotFoundException("ユーザーが見つかりません");

        if (command.Username is not null)
        {
            var newUsername = Username.Create(command.Username);
            if (!user.Username.Equals(newUsername) &&
                await _userRepository.ExistsByUsernameAsync(newUsername.Value))
                throw new ConflictException("このユーザー名はすでに使用されています");
            user.ChangeUsername(newUsername, DateTime.UtcNow);
        }

        if (command.NewPassword is not null)
        {
            if (string.IsNullOrEmpty(command.CurrentPassword))
                throw new ValidationException("現在のパスワードを入力してください");

            user.ChangePassword(
            PlainPassword.CreateForVerification(command.CurrentPassword),
            PlainPassword.Create(command.NewPassword),
            _passwordHasher,
            DateTime.UtcNow);
        }

        await _userRepository.UpdateAsync(user);
        return new UpdateMeResponse(UserDto.FromUser(user));
    }
}
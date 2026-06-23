using RunRoutes.Core.Auth.Commands.RemoveMe;
using RunRoutes.Core.Common.Commands;
using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Users;
using RunRoutes.Core.Users.Dtos;

namespace RunRoutes.Infrastructure.Commands.Auth;

public class RemoveMeCommandHandler(
    IUserRepository userRepository
) : ICommandHandler<RemoveMeCommand, DeleteAccountResponse>
{
    private readonly IUserRepository _userRepository = userRepository;

    public async Task<DeleteAccountResponse> HandleAsync(
        RemoveMeCommand command,
        CancellationToken cancellationToken
    )
    {
        var user = await _userRepository.GetByIdAsync(command.UserId)
            ?? throw new NotFoundException("ユーザーが見つかりません");

        user.MarkForRemoval(DateTime.UtcNow);
        await _userRepository.RemoveAsync(user);

        return new DeleteAccountResponse("アカウントを削除しました");
    }
}
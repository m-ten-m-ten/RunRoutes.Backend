using RunRoutes.Core.Auth.Commands.UpdateEmail;
using RunRoutes.Core.Common.Commands;
using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Users;
using RunRoutes.Core.Users.Dtos;

namespace RunRoutes.Infrastructure.Commands.Auth;

public class UpdateEmailCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IEmailService emailService
) : ICommandHandler<UpdateEmailCommand, UpdateEmailResponse>
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;
    private readonly IEmailService _emailService = emailService;

    public async Task<UpdateEmailResponse> HandleAsync(
        UpdateEmailCommand command,
        CancellationToken cancellationToken
    )
    {
        var user = await _userRepository.GetByIdForUpdateAsync(command.UserId)
            ?? throw new NotFoundException("ユーザーが見つかりません");

        var newEmail = EmailAddress.Create(command.NewEmail);
        if (await _userRepository.ExistsByEmailAsync(newEmail.Value))
            throw new ConflictException("このメールアドレスはすでに使用されています");

        user.RequestEmailChange(
            newEmail,
            PlainPassword.CreateForVerification(command.CurrentPassword),
            _passwordHasher,
            DateTime.UtcNow,
            TimeSpan.FromHours(24));

        await _userRepository.UpdateAsync(user);
        await _emailService.SendEmailChangeEmailAsync(newEmail.Value, user.EmailChange!.Token);

        return new UpdateEmailResponse("新しいメールアドレスに確認メールを送信しました");
    }
}
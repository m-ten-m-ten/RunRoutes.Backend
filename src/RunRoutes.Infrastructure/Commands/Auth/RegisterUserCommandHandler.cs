using RunRoutes.Core.Auth.Commands.RegisterUser;
using RunRoutes.Core.Common.Commands;
using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Users;
using RunRoutes.Core.Users.Dtos;

namespace RunRoutes.Infrastructure.Commands.Auth;

public class RegisterUserCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IEmailService emailService
) : ICommandHandler<RegisterUserCommand, RegisterResponse>
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;
    private readonly IEmailService _emailService = emailService;

    public async Task<RegisterResponse> HandleAsync(
        RegisterUserCommand command,
        CancellationToken cancellationToken
    )
    {
        if (await _userRepository.ExistsByEmailAsync(command.Email))
            throw new ConflictException("このメールアドレスはすでに使用されています");
        if (await _userRepository.ExistsByUsernameAsync(command.Username))
            throw new ConflictException("このユーザー名はすでに使用されています");

        var user = User.Register(
            EmailAddress.Create(command.Email),
            Username.Create(command.Username),
            PlainPassword.Create(command.Password),
            _passwordHasher,
            DateTime.UtcNow,
            TimeSpan.FromHours(24));

        await _userRepository.AddAsync(user);
        await _emailService.SendActivationEmailAsync(user.Email.Value, user.Activation!.Value);

        return new RegisterResponse("確認メールを送信しました");
    }
}
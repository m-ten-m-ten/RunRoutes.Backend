using FluentValidation;
using RunRoutes.Core.Users;

namespace RunRoutes.Core.Auth.Commands.UpdateEmail;

public class UpdateEmailCommandValidator : AbstractValidator<UpdateEmailCommand>
{
    public UpdateEmailCommandValidator()
    {
        RuleFor(x => x.NewEmail)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("メールアドレスは必須です")
            .MaximumLength(EmailAddress.MaxLength)
                .WithMessage($"メールアドレスは {EmailAddress.MaxLength} 文字以下にしてください")
            .Must(e => EmailAddress.Pattern.IsMatch(e.Trim()))
                .WithMessage("メールアドレスの形式が正しくありません")
            .OverridePropertyName("newEmail");

        // 照合用パスワードなので長さポリシーは課さない。
        // Handler が PlainPassword.CreateForVerification を使っており、
        // ポリシー変更前の短いパスワードのユーザーを締め出さないための意図的な緩和。
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("現在のパスワードを入力してください")
            .OverridePropertyName("currentPassword");
    }
}
using FluentValidation;
using RunRoutes.Core.Users;

namespace RunRoutes.Core.Auth.Commands.RegisterUser;

public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        // 検証内容は EmailAddress / Username / PlainPassword の不変条件と重複するが、
        // 目的が異なる（門番として 400 + フィールドキーを返す vs 壊れた値を存在させない）。
        // 正規表現・長さ・予約語は値オブジェクト側の定義を参照し、二重定義を避ける。
        // 重複チェック（DB 照会）は Handler の担当。

        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("メールアドレスは必須です")
            .MaximumLength(EmailAddress.MaxLength)
                .WithMessage($"メールアドレスは {EmailAddress.MaxLength} 文字以下にしてください")
            .Must(e => EmailAddress.Pattern.IsMatch(e.Trim()))
                .WithMessage("メールアドレスの形式が正しくありません")
            .OverridePropertyName("email");

        RuleFor(x => x.Username)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("ユーザー名は必須です")
            .Must(u => u.Trim().Length >= Username.MinLength && u.Trim().Length <= Username.MaxLength)
                .WithMessage($"ユーザー名は {Username.MinLength}〜{Username.MaxLength} 文字で入力してください")
            .Must(u => Username.Pattern.IsMatch(u.Trim()))
                .WithMessage("ユーザー名は英数字・アンダースコア・ハイフンのみ使用できます")
            .Must(u => !Username.IsReserved(u.Trim().ToLowerInvariant()))
                .WithMessage("このユーザー名は使用できません")
            .OverridePropertyName("username");

        RuleFor(x => x.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("パスワードは必須です")
            .MinimumLength(PlainPassword.MinLength)
                .WithMessage($"パスワードは {PlainPassword.MinLength} 文字以上で入力してください")
            .MaximumLength(PlainPassword.MaxLength)
                .WithMessage($"パスワードは {PlainPassword.MaxLength} 文字以下で入力してください")
            .OverridePropertyName("password");
    }
}
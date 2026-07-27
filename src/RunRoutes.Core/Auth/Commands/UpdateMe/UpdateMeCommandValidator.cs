using FluentValidation;
using RunRoutes.Core.Users;

namespace RunRoutes.Core.Auth.Commands.UpdateMe;

public class UpdateMeCommandValidator : AbstractValidator<UpdateMeCommand>
{
    public UpdateMeCommandValidator()
    {
        // 部分更新: null は「変更しない」の意味なので検証対象外。
        // username / password は独立して指定できる（両方 null も正当なリクエスト）。
        When(x => x.Username is not null, () =>
        {
            RuleFor(x => x.Username!)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("ユーザー名は必須です")
                .Must(u => u.Trim().Length >= Username.MinLength && u.Trim().Length <= Username.MaxLength)
                    .WithMessage($"ユーザー名は {Username.MinLength}〜{Username.MaxLength} 文字で入力してください")
                .Must(u => Username.Pattern.IsMatch(u.Trim()))
                    .WithMessage("ユーザー名は英数字・アンダースコア・ハイフンのみ使用できます")
                .Must(u => !Username.IsReserved(u.Trim().ToLowerInvariant()))
                    .WithMessage("このユーザー名は使用できません")
                .OverridePropertyName("username");
        });

        // NewPassword が指定されたときだけ、新パスワードのポリシーと
        // CurrentPassword の同時指定を要求する（Handler の分岐と対応）。
        When(x => x.NewPassword is not null, () =>
        {
            RuleFor(x => x.NewPassword!)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("パスワードは必須です")
                .MinimumLength(PlainPassword.MinLength)
                    .WithMessage($"パスワードは {PlainPassword.MinLength} 文字以上で入力してください")
                .MaximumLength(PlainPassword.MaxLength)
                    .WithMessage($"パスワードは {PlainPassword.MaxLength} 文字以下で入力してください")
                .OverridePropertyName("newPassword");

            RuleFor(x => x.CurrentPassword)
                .NotEmpty().WithMessage("現在のパスワードを入力してください")
                .OverridePropertyName("currentPassword");
        });
    }
}
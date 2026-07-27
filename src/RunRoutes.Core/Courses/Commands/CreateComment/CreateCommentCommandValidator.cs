using FluentValidation;

namespace RunRoutes.Core.Courses.Commands.CreateComment;

public class CreateCommentCommandValidator : AbstractValidator<CreateCommentCommand>
{
    public CreateCommentCommandValidator()
    {
        // 必須チェックは Comment.Create の不変条件と重複するが、目的が異なる
        // （門番として 400 + フィールドキーを返す vs 壊れた値を存在させない）。
        // 長さ上限はドメインに存在しない入口固有の制限。既存データの移行を避けるため
        // ドメインには入れず、外部入力に対してのみ課している。
        RuleFor(x => x.Body)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("コメント本文は必須です")
            .MaximumLength(CommentBodyRules.MaxLength)
                .WithMessage($"コメントは {CommentBodyRules.MaxLength} 文字以内で入力してください")
            .OverridePropertyName("body");
    }
}
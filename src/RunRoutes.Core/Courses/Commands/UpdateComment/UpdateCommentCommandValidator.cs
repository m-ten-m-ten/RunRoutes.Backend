using FluentValidation;

namespace RunRoutes.Core.Courses.Commands.UpdateComment;

public class UpdateCommentCommandValidator : AbstractValidator<UpdateCommentCommand>
{
    // 検証内容は CreateComment と同一（Comment.UpdateBody の不変条件も Create と同じ）。
    // 上限値は CommentBodyRules に一本化しているため、2つの Validator でズレることはない。
    public UpdateCommentCommandValidator()
    {
        RuleFor(x => x.Body)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("コメント本文は必須です")
            .MaximumLength(CommentBodyRules.MaxLength)
                .WithMessage($"コメントは {CommentBodyRules.MaxLength} 文字以内で入力してください")
            .OverridePropertyName("body");
    }
}
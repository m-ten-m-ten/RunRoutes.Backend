using FluentValidation;

namespace RunRoutes.Core.Courses.Commands.UpdateCourse;

public class UpdateCourseCommandValidator : AbstractValidator<UpdateCourseCommand>
{
    public UpdateCourseCommandValidator()
    {
        // 部分更新のため null は検証対象外とする（各項目を「変更しない/クリアする」の
        // どちらとして扱うかは Handler の責務）。非 null のときだけ値の妥当性を見る。
        When(x => x.Title is not null, () =>
        {
            RuleFor(x => x.Title!)
                .NotEmpty()
                .OverridePropertyName("title")
                .WithMessage("タイトルは必須です");
        });

        When(x => x.Difficulty is not null, () =>
        {
            RuleFor(x => x.Difficulty!)
                .Must(d => DifficultyNames.IsValid(d))
                .OverridePropertyName("difficulty")
                .WithMessage($"{DifficultyNames.AllowedText} のいずれかを指定してください");
        });

        When(x => x.Route is not null, () =>
        {
            RuleFor(x => x.Route!)
                .Must(r => r.Coordinates is not null && r.Coordinates.Count() >= 2)
                .OverridePropertyName("route")
                .WithMessage("ルートには2点以上の座標が必要です");
        });

        // Handler が GpxParser.Parse に到達するのは Route が null のときだけ。
        // Route 指定時の空 gpxXml は Handler が無視するので、ここでも弾かない。
        When(x => x.Route is null && x.GpxXml is not null, () =>
        {
            RuleFor(x => x.GpxXml!)
                .NotEmpty()
                .OverridePropertyName("route")
                .WithMessage("route または gpxXml のいずれかを指定してください");
        });
    }
}
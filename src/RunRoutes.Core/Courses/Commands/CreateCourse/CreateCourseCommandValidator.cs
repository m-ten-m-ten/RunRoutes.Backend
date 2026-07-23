using FluentValidation;

namespace RunRoutes.Core.Courses.Commands.CreateCourse;

public class CreateCourseCommandValidator : AbstractValidator<CreateCourseCommand>
{
    public CreateCourseCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .OverridePropertyName("title")
            .WithMessage("タイトルは必須です");

        RuleFor(x => x.Difficulty)
            .Must(d => DifficultyNames.IsValid(d))
            .OverridePropertyName("difficulty")
            .WithMessage($"{DifficultyNames.AllowedText} のいずれかを指定してください");

        RuleFor(x => x)
            .Must(x => x.Route is not null || !string.IsNullOrWhiteSpace(x.GpxXml))
            .OverridePropertyName("route")
            .WithMessage("route または gpxXml のいずれかを指定してください");

        When(x => x.Route is not null, () =>
        {
            RuleFor(x => x.Route!)
                .Must(r => r.Coordinates is not null && r.Coordinates.Count() >= 2)
                .OverridePropertyName("route")
                .WithMessage("ルートには2点以上の座標が必要です");
        });
    }
}
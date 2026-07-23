using FluentValidation;

namespace RunRoutes.Core.Courses.Commands.CreateCourse;

public class CreateCourseCommandValidator : AbstractValidator<CreateCourseCommand>
{
    private static readonly string[] AllowedDifficulties = ["easy", "medium", "hard"];

    public CreateCourseCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .OverridePropertyName("title")
            .WithMessage("タイトルは必須です");

        RuleFor(x => x.Difficulty)
            .Must(d => AllowedDifficulties.Contains(d, StringComparer.OrdinalIgnoreCase))
            .OverridePropertyName("difficulty")
            .WithMessage("easy, medium, hard のいずれかを指定してください");

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
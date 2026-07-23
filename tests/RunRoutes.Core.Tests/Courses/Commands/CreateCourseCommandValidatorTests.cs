using FluentValidation.TestHelper;
using RunRoutes.Core.Courses.Commands.CreateCourse;
using RunRoutes.Core.Courses.Dtos;

namespace RunRoutes.Core.Tests.Courses.Commands;

public class CreateCourseCommandValidatorTests
{
    private readonly CreateCourseCommandValidator _validator = new();

    private static CreateCourseCommand ValidCommand() => new(
        Title: "テストコース",
        Description: null,
        Difficulty: "easy",
        IsPublic: true,
        Route: new GeoJsonLineStringDto("LineString", [[139.7, 35.6], [139.8, 35.7]]),
        GpxXml: null,
        TagIds: [],
        UserId: Guid.NewGuid());

    [Fact]
    public void 正常なコマンドはエラーにならない()
    {
        var result = _validator.TestValidate(ValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void タイトルが空ならエラー()
    {
        var command = ValidCommand() with { Title = "" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor("title");
    }

    [Fact]
    public void 不正な難易度ならエラー()
    {
        var command = ValidCommand() with { Difficulty = "invalid" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor("difficulty");
    }

    [Fact]
    public void routeもgpxXmlも無いならエラー()
    {
        var command = ValidCommand() with { Route = null, GpxXml = null };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor("route");
    }

    [Fact]
    public void 座標が2点未満ならエラー()
    {
        var command = ValidCommand() with
        {
            Route = new GeoJsonLineStringDto("LineString", [[139.7, 35.6]])
        };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor("route");
    }
}
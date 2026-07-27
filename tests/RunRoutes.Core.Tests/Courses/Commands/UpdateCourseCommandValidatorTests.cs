using FluentValidation.TestHelper;
using RunRoutes.Core.Courses.Commands.UpdateCourse;
using RunRoutes.Core.Courses.Dtos;

namespace RunRoutes.Core.Tests.Courses.Commands;

public class UpdateCourseCommandValidatorTests
{
    private readonly UpdateCourseCommandValidator _validator = new();

    private static UpdateCourseCommand ValidCommand() => new(
        Id: Guid.NewGuid(),
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
    public void 全項目未指定の部分更新はエラーにならない()
    {
        var command = ValidCommand() with
        {
            Title = null,
            Difficulty = null,
            IsPublic = null,
            Route = null,
            GpxXml = null,
            TagIds = null
        };
        var result = _validator.TestValidate(command);
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
    public void 座標が2点未満ならエラー()
    {
        var command = ValidCommand() with
        {
            Route = new GeoJsonLineStringDto("LineString", [[139.7, 35.6]])
        };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor("route");
    }

    [Fact]
    public void routeが無くgpxXmlが空文字ならエラー()
    {
        var command = ValidCommand() with { Route = null, GpxXml = "" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor("route");
    }
}
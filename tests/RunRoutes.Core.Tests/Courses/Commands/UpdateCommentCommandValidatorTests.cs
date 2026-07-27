using FluentValidation.TestHelper;
using RunRoutes.Core.Courses;
using RunRoutes.Core.Courses.Commands.UpdateComment;

namespace RunRoutes.Core.Tests.Courses.Commands;

public class UpdateCommentCommandValidatorTests
{
    private readonly UpdateCommentCommandValidator _validator = new();

    private static UpdateCommentCommand ValidCommand() => new(
        CourseId: Guid.NewGuid(),
        CommentId: Guid.NewGuid(),
        UserId: Guid.NewGuid(),
        Body: "編集後の本文");

    [Fact]
    public void 正常なコマンドはエラーにならない()
    {
        var result = _validator.TestValidate(ValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void 本文が空ならエラー()
    {
        var result = _validator.TestValidate(ValidCommand() with { Body = "" });
        result.ShouldHaveValidationErrorFor("body");
    }

    [Fact]
    public void 本文が空白のみならエラー()
    {
        var result = _validator.TestValidate(ValidCommand() with { Body = "   " });
        result.ShouldHaveValidationErrorFor("body");
    }

    [Fact]
    public void 本文が長すぎるならエラー()
    {
        var command = ValidCommand() with
        {
            Body = new string('あ', CommentBodyRules.MaxLength + 1)
        };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor("body");
    }

    [Fact]
    public void 上限ちょうどならエラーにならない()
    {
        var command = ValidCommand() with
        {
            Body = new string('あ', CommentBodyRules.MaxLength)
        };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
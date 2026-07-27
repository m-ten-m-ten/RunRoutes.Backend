using FluentValidation.TestHelper;
using RunRoutes.Core.Auth.Commands.UpdateEmail;

namespace RunRoutes.Core.Tests.Auth.Commands;

public class UpdateEmailCommandValidatorTests
{
    private readonly UpdateEmailCommandValidator _validator = new();

    private static UpdateEmailCommand ValidCommand() => new(
        UserId: Guid.NewGuid(),
        NewEmail: "new@example.com",
        CurrentPassword: "old");

    [Fact]
    public void 正常なコマンドはエラーにならない()
    {
        var result = _validator.TestValidate(ValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void メールアドレスが空ならエラー()
    {
        var result = _validator.TestValidate(ValidCommand() with { NewEmail = "" });
        result.ShouldHaveValidationErrorFor("newEmail");
    }

    [Fact]
    public void メールアドレスの形式が不正ならエラー()
    {
        var result = _validator.TestValidate(ValidCommand() with { NewEmail = "not-an-email" });
        result.ShouldHaveValidationErrorFor("newEmail");
    }

    [Fact]
    public void 現在のパスワードが空ならエラー()
    {
        var result = _validator.TestValidate(ValidCommand() with { CurrentPassword = "" });
        result.ShouldHaveValidationErrorFor("currentPassword");
    }
}
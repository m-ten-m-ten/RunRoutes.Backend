using FluentValidation.TestHelper;
using RunRoutes.Core.Auth.Commands.UpdateMe;

namespace RunRoutes.Core.Tests.Auth.Commands;

public class UpdateMeCommandValidatorTests
{
    private readonly UpdateMeCommandValidator _validator = new();

    private static UpdateMeCommand ValidCommand() => new(
        UserId: Guid.NewGuid(),
        Username: "runner_01",
        CurrentPassword: null,
        NewPassword: null);

    [Fact]
    public void ユーザー名のみ指定はエラーにならない()
    {
        var result = _validator.TestValidate(ValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void 全項目未指定はエラーにならない()
    {
        var result = _validator.TestValidate(ValidCommand() with { Username = null });
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void パスワード変更時に現パスワードと新パスワードが揃っていればエラーにならない()
    {
        var command = ValidCommand() with
        {
            Username = null,
            CurrentPassword = "old",
            NewPassword = "newpassword123"
        };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void 新パスワードのみ指定で現パスワードが無いならエラー()
    {
        var command = ValidCommand() with { Username = null, NewPassword = "newpassword123" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor("currentPassword");
    }

    [Fact]
    public void 新パスワードが短すぎるならエラー()
    {
        var command = ValidCommand() with
        {
            Username = null,
            CurrentPassword = "old",
            NewPassword = "short"
        };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor("newPassword");
    }

    [Fact]
    public void ユーザー名が不正ならエラー()
    {
        var result = _validator.TestValidate(ValidCommand() with { Username = "ab" });
        result.ShouldHaveValidationErrorFor("username");
    }
}
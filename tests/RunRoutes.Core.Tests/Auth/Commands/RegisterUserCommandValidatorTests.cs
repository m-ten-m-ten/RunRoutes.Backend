using FluentValidation.TestHelper;
using RunRoutes.Core.Auth.Commands.RegisterUser;

namespace RunRoutes.Core.Tests.Auth.Commands;

public class RegisterUserCommandValidatorTests
{
    private readonly RegisterUserCommandValidator _validator = new();

    private static RegisterUserCommand ValidCommand() => new(
        Email: "user@example.com",
        Username: "runner_01",
        Password: "password123");

    [Fact]
    public void 正常なコマンドはエラーにならない()
    {
        var result = _validator.TestValidate(ValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void メールアドレスが空ならエラー()
    {
        var result = _validator.TestValidate(ValidCommand() with { Email = "" });
        result.ShouldHaveValidationErrorFor("email");
    }

    [Fact]
    public void メールアドレスの形式が不正ならエラー()
    {
        var result = _validator.TestValidate(ValidCommand() with { Email = "not-an-email" });
        result.ShouldHaveValidationErrorFor("email");
    }

    [Fact]
    public void ユーザー名が短すぎるならエラー()
    {
        var result = _validator.TestValidate(ValidCommand() with { Username = "ab" });
        result.ShouldHaveValidationErrorFor("username");
    }

    [Fact]
    public void ユーザー名に使用できない文字が含まれるならエラー()
    {
        var result = _validator.TestValidate(ValidCommand() with { Username = "runner 01" });
        result.ShouldHaveValidationErrorFor("username");
    }

    [Fact]
    public void 予約されたユーザー名ならエラー()
    {
        var result = _validator.TestValidate(ValidCommand() with { Username = "Admin" });
        result.ShouldHaveValidationErrorFor("username");
    }

    [Fact]
    public void パスワードが短すぎるならエラー()
    {
        var result = _validator.TestValidate(ValidCommand() with { Password = "pass123" });
        result.ShouldHaveValidationErrorFor("password");
    }
}
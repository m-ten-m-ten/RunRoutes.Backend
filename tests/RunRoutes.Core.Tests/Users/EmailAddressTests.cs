using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Users;

namespace RunRoutes.Core.Tests.Users;

public class EmailAddressTests
{
    [Theory]
    [InlineData("user@example.com")]
    [InlineData("USER@EXAMPLE.COM")]
    [InlineData("a@b.co")]
    public void Create_正常系(string input)
    {
        var email = EmailAddress.Create(input);
        Assert.Equal(input.ToLowerInvariant(), email.Value);
    }

    [Fact]
    public void Create_大文字は小文字化される()
    {
        var email = EmailAddress.Create("Test@Example.COM");
        Assert.Equal("test@example.com", email.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_空または空白でValidationException(string? input)
    {
        Assert.Throws<ValidationException>(() => EmailAddress.Create(input!));
    }

    [Fact]
    public void Create_254文字超でValidationException()
    {
        var longEmail = new string('a', 249) + "@b.com"; // 255文字(249+6)
        Assert.Throws<ValidationException>(() => EmailAddress.Create(longEmail));
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("missing@dot")]
    [InlineData("@nodomain.com")]
    [InlineData("space @example.com")]
    public void Create_形式不正でValidationException(string input)
    {
        Assert.Throws<ValidationException>(() => EmailAddress.Create(input));
    }

    [Fact]
    public void ToString_Valueを返す()
    {
        var email = EmailAddress.Create("user@example.com");
        Assert.Equal("user@example.com", email.ToString());
    }

    [Fact]
    public void 等価性_同じアドレスは等価()
    {
        var a = EmailAddress.Create("user@example.com");
        var b = EmailAddress.Create("User@Example.COM");
        Assert.Equal(a, b);
    }
}

using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Users;

namespace RunRoutes.Core.Tests.Users;

public class UsernameTests
{
    [Theory]
    [InlineData("abc")]
    [InlineData("testuser")]
    [InlineData("Test_User-123")]
    [InlineData("a23456789012345678901234567890")] // 30文字
    public void Create_正常系(string input)
    {
        var username = Username.Create(input);
        Assert.Equal(input.Trim(), username.Value);
        Assert.Equal(input.Trim().ToLowerInvariant(), username.Normalized);
    }

    [Fact]
    public void Create_大文字はValueに保持されNormalizedは小文字()
    {
        var username = Username.Create("TestUser");
        Assert.Equal("TestUser", username.Value);
        Assert.Equal("testuser", username.Normalized);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_空または空白でValidationException(string? input)
    {
        Assert.Throws<ValidationException>(() => Username.Create(input!));
    }

    [Theory]
    [InlineData("ab")]           // 2文字以下
    [InlineData("a234567890123456789012345678901")] // 31文字
    public void Create_長さ違反でValidationException(string input)
    {
        Assert.Throws<ValidationException>(() => Username.Create(input));
    }

    [Theory]
    [InlineData("user name")]    // スペース
    [InlineData("user@name")]    // @
    [InlineData("user.name")]    // ドット
    public void Create_記号含みでValidationException(string input)
    {
        Assert.Throws<ValidationException>(() => Username.Create(input));
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("root")]
    [InlineData("api")]
    [InlineData("system")]
    [InlineData("support")]
    [InlineData("me")]
    [InlineData("ADMIN")]        // 大文字予約語
    public void Create_予約語でValidationException(string input)
    {
        Assert.Throws<ValidationException>(() => Username.Create(input));
    }

    [Fact]
    public void 等価性_Normalizedベースで比較される()
    {
        var a = Username.Create("TestUser");
        var b = Username.Create("testuser");
        Assert.Equal(a, b);
    }

    [Fact]
    public void ToString_Valueを返す()
    {
        var username = Username.Create("TestUser");
        Assert.Equal("TestUser", username.ToString());
    }
}

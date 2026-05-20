using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Users;

namespace RunRoutes.Core.Tests.Users;

public class PlainPasswordTests
{
    [Theory]
    [InlineData("password")]         // 8文字
    [InlineData("password123")]
    [InlineData("a!@#$%^&*()_+{}")]
    public void Create_正常系(string input)
    {
        var plain = PlainPassword.Create(input);
        Assert.Equal(input, plain.Value);
    }

    [Fact]
    public void Create_100文字はOK()
    {
        var plain = PlainPassword.Create(new string('a', 100));
        Assert.Equal(100, plain.Value.Length);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Create_空でValidationException(string? input)
    {
        Assert.Throws<ValidationException>(() => PlainPassword.Create(input!));
    }

    [Fact]
    public void Create_7文字以下でValidationException()
    {
        Assert.Throws<ValidationException>(() => PlainPassword.Create("1234567"));
    }

    [Fact]
    public void Create_101文字以上でValidationException()
    {
        Assert.Throws<ValidationException>(() => PlainPassword.Create(new string('a', 101)));
    }

    [Theory]
    [InlineData("a")]
    [InlineData("1234567")]          // 7文字（Create では弾かれる長さ）
    [InlineData("password123")]
    public void CreateForVerification_長さに関わらず通る(string input)
    {
        var plain = PlainPassword.CreateForVerification(input);
        Assert.Equal(input, plain.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void CreateForVerification_空でValidationException(string? input)
    {
        Assert.Throws<ValidationException>(() => PlainPassword.CreateForVerification(input!));
    }

    [Fact]
    public void ToString_マスクされる()
    {
        var plain = PlainPassword.Create("password123");
        Assert.Equal("***", plain.ToString());
    }
}

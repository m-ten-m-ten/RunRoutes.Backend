using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Users;

namespace RunRoutes.Core.Tests.Users;

public class HashedPasswordTests
{
    [Theory]
    [InlineData("$2a$10$abcdefghijklmnopqrstuuVGtByGRUm4RGrGkHkG3fW1ncuknG9HW")]
    [InlineData("$2b$12$somevalidhashstringhere1234567890")]
    [InlineData("$2y$10$somevalidhashstringhere1234567890")]
    public void FromHash_正常系(string hash)
    {
        var hp = HashedPassword.FromHash(hash);
        Assert.Equal(hash, hp.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FromHash_空でValidationException(string? hash)
    {
        Assert.Throws<ValidationException>(() => HashedPassword.FromHash(hash!));
    }

    [Theory]
    [InlineData("plainpassword")]
    [InlineData("$1$notbcrypt")]
    [InlineData("sha256:notsupported")]
    public void FromHash_BCrypt形式でなければValidationException(string hash)
    {
        Assert.Throws<ValidationException>(() => HashedPassword.FromHash(hash));
    }

    [Fact]
    public void ToString_マスクされる()
    {
        var hp = HashedPassword.FromHash("$2a$10$abcdefghijklmnopqrstuuVGtByGRUm4RGrGkHkG3fW1ncuknG9HW");
        Assert.Equal("***", hp.ToString());
    }
}

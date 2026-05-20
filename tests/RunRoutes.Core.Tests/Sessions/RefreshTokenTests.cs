using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Sessions;

namespace RunRoutes.Core.Tests.Sessions;

public class RefreshTokenTests
{
    [Fact]
    public void Generate_正常に生成できる()
    {
        var now = DateTime.UtcNow;
        var validity = TimeSpan.FromDays(7);

        var token = RefreshToken.Generate(now, validity);

        Assert.NotEmpty(token.Value);
        Assert.Equal(now + validity, token.ExpiresAt);
    }

    [Fact]
    public void Generate_値がランダムで毎回異なる()
    {
        var now = DateTime.UtcNow;
        var t1 = RefreshToken.Generate(now, TimeSpan.FromDays(1));
        var t2 = RefreshToken.Generate(now, TimeSpan.FromDays(1));

        Assert.NotEqual(t1.Value, t2.Value);
    }

    [Fact]
    public void Generate_有効期間0以下でValidationException()
    {
        var now = DateTime.UtcNow;

        Assert.Throws<ValidationException>(() =>
            RefreshToken.Generate(now, TimeSpan.Zero));
        Assert.Throws<ValidationException>(() =>
            RefreshToken.Generate(now, TimeSpan.FromSeconds(-1)));
    }

    [Fact]
    public void FromStorage_値とExpiresAtが復元できる()
    {
        var expiresAt = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var token = RefreshToken.FromStorage("abc123", expiresAt);

        Assert.Equal("abc123", token.Value);
        Assert.Equal(expiresAt, token.ExpiresAt);
    }

    [Fact]
    public void IsExpired_期限前はfalse()
    {
        var now = DateTime.UtcNow;
        var token = RefreshToken.Generate(now, TimeSpan.FromDays(7));

        Assert.False(token.IsExpired(now));
        Assert.False(token.IsExpired(now.AddDays(6)));
    }

    [Fact]
    public void IsExpired_期限到達でtrue()
    {
        var now = DateTime.UtcNow;
        var token = RefreshToken.Generate(now, TimeSpan.FromDays(7));

        Assert.True(token.IsExpired(now.AddDays(7)));
        Assert.True(token.IsExpired(now.AddDays(8)));
    }
}

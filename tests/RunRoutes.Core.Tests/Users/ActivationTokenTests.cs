using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Users;

namespace RunRoutes.Core.Tests.Users;

public class ActivationTokenTests
{
    [Fact]
    public void Generate_正常に生成できる()
    {
        var now = DateTime.UtcNow;
        var validity = TimeSpan.FromHours(24);

        var token = ActivationToken.Generate(now, validity);

        Assert.NotEmpty(token.Value);
        Assert.Equal(now + validity, token.ExpiresAt);
    }

    [Fact]
    public void Generate_URLセーフでない文字を含まない()
    {
        // URL クエリ文字列で '+' は半角スペースに化けるため、
        // '+' '/' '=' を含まない Base64Url であること
        var token = ActivationToken.Generate(DateTime.UtcNow, TimeSpan.FromHours(1));

        Assert.DoesNotContain('+', token.Value);
        Assert.DoesNotContain('/', token.Value);
        Assert.DoesNotContain('=', token.Value);
    }

    [Fact]
    public void Generate_値がランダムで毎回異なる()
    {
        var now = DateTime.UtcNow;
        var t1 = ActivationToken.Generate(now, TimeSpan.FromHours(1));
        var t2 = ActivationToken.Generate(now, TimeSpan.FromHours(1));

        Assert.NotEqual(t1.Value, t2.Value);
    }

    [Fact]
    public void Generate_有効期間0以下でValidationException()
    {
        var now = DateTime.UtcNow;

        Assert.Throws<ValidationException>(() =>
            ActivationToken.Generate(now, TimeSpan.Zero));
        Assert.Throws<ValidationException>(() =>
            ActivationToken.Generate(now, TimeSpan.FromSeconds(-1)));
    }

    [Fact]
    public void FromStorage_値とExpiresAtが復元できる()
    {
        var expiresAt = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var token = ActivationToken.FromStorage("abc123", expiresAt);

        Assert.Equal("abc123", token.Value);
        Assert.Equal(expiresAt, token.ExpiresAt);
    }

    [Fact]
    public void IsExpired_期限前はfalse()
    {
        var now = DateTime.UtcNow;
        var token = ActivationToken.Generate(now, TimeSpan.FromHours(1));

        Assert.False(token.IsExpired(now));
        Assert.False(token.IsExpired(now.AddMinutes(59)));
    }

    [Fact]
    public void IsExpired_期限到達でtrue()
    {
        var now = DateTime.UtcNow;
        var token = ActivationToken.Generate(now, TimeSpan.FromHours(1));

        Assert.True(token.IsExpired(now.AddHours(1)));
        Assert.True(token.IsExpired(now.AddHours(2)));
    }
}

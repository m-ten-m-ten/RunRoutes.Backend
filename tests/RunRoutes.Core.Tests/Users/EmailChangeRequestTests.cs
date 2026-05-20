using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Users;

namespace RunRoutes.Core.Tests.Users;

public class EmailChangeRequestTests
{
    private static readonly EmailAddress SomeEmail = EmailAddress.Create("new@example.com");

    // ========================================
    // EmailChangeRequest.Create
    // ========================================

    [Fact]
    public void Create_正常にEmailChangeRequestが生成される()
    {
        var now = DateTime.UtcNow;
        var validity = TimeSpan.FromHours(24);

        var req = EmailChangeRequest.Create(SomeEmail, now, validity);

        Assert.Equal(SomeEmail, req.NewEmail);
        Assert.NotEmpty(req.Token);
        Assert.Equal(now + validity, req.ExpiresAt);
    }

    [Fact]
    public void Create_複数呼び出しで異なるTokenが生成される()
    {
        var now = DateTime.UtcNow;
        var validity = TimeSpan.FromHours(24);

        var req1 = EmailChangeRequest.Create(SomeEmail, now, validity);
        var req2 = EmailChangeRequest.Create(SomeEmail, now, validity);

        Assert.NotEqual(req1.Token, req2.Token);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_無効な有効期間でValidationException(int seconds)
    {
        Assert.Throws<ValidationException>(() =>
            EmailChangeRequest.Create(SomeEmail, DateTime.UtcNow, TimeSpan.FromSeconds(seconds)));
    }

    // ========================================
    // EmailChangeRequest.IsExpired
    // ========================================

    [Fact]
    public void IsExpired_期限前はfalse()
    {
        var now = DateTime.UtcNow;
        var req = EmailChangeRequest.Create(SomeEmail, now, TimeSpan.FromHours(24));

        Assert.False(req.IsExpired(now));
        Assert.False(req.IsExpired(now.AddHours(23).AddMinutes(59)));
    }

    [Fact]
    public void IsExpired_期限到達でtrue()
    {
        var now = DateTime.UtcNow;
        var validity = TimeSpan.FromHours(24);
        var req = EmailChangeRequest.Create(SomeEmail, now, validity);

        Assert.True(req.IsExpired(now + validity));
        Assert.True(req.IsExpired(now.AddHours(25)));
    }
}

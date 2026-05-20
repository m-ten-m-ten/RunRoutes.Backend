using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Courses;

namespace RunRoutes.Core.Tests.Courses;

public class DistanceTests
{
    [Fact]
    public void FromMeters_正の値で生成できる()
    {
        var d = Distance.FromMeters(1500);
        Assert.Equal(1500, d.Meters);
        Assert.Equal(1.5, d.Kilometers);
    }

    [Fact]
    public void FromMeters_ゼロで生成できる()
    {
        var d = Distance.FromMeters(0);
        Assert.Equal(0, d.Meters);
        Assert.Equal(0, d.Kilometers);
    }

    [Fact]
    public void FromMeters_負の値でValidationException()
    {
        Assert.Throws<ValidationException>(() => Distance.FromMeters(-1));
    }

    [Fact]
    public void FromMeters_NaNでValidationException()
    {
        Assert.Throws<ValidationException>(() => Distance.FromMeters(double.NaN));
    }

    [Fact]
    public void FromMeters_InfinityでValidationException()
    {
        Assert.Throws<ValidationException>(() => Distance.FromMeters(double.PositiveInfinity));
    }

    [Fact]
    public void 同じMetersなら等価()
    {
        var a = Distance.FromMeters(1000);
        var b = Distance.FromMeters(1000);
        Assert.Equal(a, b);
        Assert.True(a == b);
    }

    [Fact]
    public void 異なるMetersなら非等価()
    {
        var a = Distance.FromMeters(1000);
        var b = Distance.FromMeters(2000);
        Assert.NotEqual(a, b);
    }
}

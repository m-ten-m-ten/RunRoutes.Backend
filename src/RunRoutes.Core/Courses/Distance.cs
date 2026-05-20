using RunRoutes.Core.Common.Exceptions;

namespace RunRoutes.Core.Courses;

public sealed record Distance
{
    public double Meters { get; }
    public double Kilometers => Meters / 1000.0;

    private Distance(double meters)
    {
        Meters = meters;
    }

    public static Distance FromMeters(double meters)
    {
        if (double.IsNaN(meters) || double.IsInfinity(meters))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["distance"] = ["距離は有限の数値である必要があります"]
            });
        }

        if (meters < 0)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["distance"] = ["距離は0以上である必要があります"]
            });
        }

        return new Distance(meters);
    }
}

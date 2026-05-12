using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RunRoutes.Core.Courses;

namespace RunRoutes.Infrastructure.Configurations.Converters;

public sealed class DistanceConverter : ValueConverter<Distance, double>
{
    public DistanceConverter() : base(
        v => v.Meters,
        v => Distance.FromMeters(v))
    {
    }
}

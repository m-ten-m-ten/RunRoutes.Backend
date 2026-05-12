using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RunRoutes.Core.Courses;

namespace RunRoutes.Infrastructure.Configurations.Converters;

public sealed class DifficultyConverter : ValueConverter<Difficulty, string>
{
    public DifficultyConverter() : base(
        v => v.ToString().ToLowerInvariant(),
        v => Enum.Parse<Difficulty>(v, true))
    {
    }
}

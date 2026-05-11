namespace RunRoutes.Core.Courses.Dtos;

public record GeoJsonLineStringDto(string Type, IEnumerable<double[]> Coordinates);

using System.Text.Json.Serialization;

namespace RunRoutes.Core.DTOs.Common;

public record UserDto(Guid Id, string Email, string Username, string Role, DateTime CreatedAt);
public record TagDto(Guid Id, string Name);
public record GeoJsonLineStringDto(string Type, IEnumerable<double[]> Coordinates);
public record ErrorResponse(
    string Message,
    IDictionary<string, string[]>? Errors = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Code = null);

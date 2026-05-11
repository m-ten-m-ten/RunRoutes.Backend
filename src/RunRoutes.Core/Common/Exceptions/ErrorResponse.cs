using System.Text.Json.Serialization;

namespace RunRoutes.Core.Common.Exceptions;

public record ErrorResponse(
    string Message,
    IDictionary<string, string[]>? Errors = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Code = null);

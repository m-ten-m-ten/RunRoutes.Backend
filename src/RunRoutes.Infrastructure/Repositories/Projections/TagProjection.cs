using RunRoutes.Core.Tags;

namespace RunRoutes.Infrastructure.Repositories.Projections;

internal sealed class TagProjection
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public uint Version { get; init; }

    public Tag ToDomain() => Tag.Reconstruct(Id, Name, CreatedAt, Version);
}
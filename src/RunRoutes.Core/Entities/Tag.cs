namespace RunRoutes.Core.Entities;

public class Tag
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public uint Version { get; set; }

    public ICollection<Course> Courses { get; set; } = [];
}

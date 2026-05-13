// RunRoutes.Core/Tags/Tag.cs
using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Courses;

namespace RunRoutes.Core.Tags;

public class Tag
{
    public const int MaxNameLength = 50;

    // EF Core 用の parameterless constructor
    private Tag() { }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public uint Version { get; private set; }

    public ICollection<Course> Courses { get; private set; } = [];

    // ========================================
    // ファクトリメソッド(新規生成)
    // ========================================
    public static Tag Create(string name)
    {
        var normalized = NormalizeName(name);
        return new Tag
        {
            Id = Guid.NewGuid(),
            Name = normalized,
            CreatedAt = DateTime.UtcNow,
            // Version は EF Core が xmin から自動採番するので初期値 0 のまま
        };
    }

    // ========================================
    // 再構成メソッド(EF Core / Repository / テスト用)
    // ========================================
    internal static Tag Reconstruct(
        Guid id,
        string name,
        DateTime createdAt,
        uint version)
    {
        return new Tag
        {
            Id = id,
            Name = name,
            CreatedAt = createdAt,
            Version = version,
        };
    }

    // ========================================
    // ドメインメソッド(状態変更)
    // ========================================
    public void Rename(string newName)
    {
        var normalized = NormalizeName(newName);

        // 同名なら何もしない(early return、不要な UPDATE を回避)
        if (string.Equals(Name, normalized, StringComparison.Ordinal))
            return;

        Name = normalized;
    }

    // ========================================
    // 内部ヘルパー
    // ========================================
    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["name"] = ["名前は必須です"]
            });

        var trimmed = name.Trim();
        if (trimmed.Length > MaxNameLength)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["name"] = [$"名前は {MaxNameLength} 文字以内で入力してください"]
            });

        return trimmed;
    }

    internal static string NormalizeNameForService(string name) => NormalizeName(name);
}
namespace RunRoutes.Core.Courses;

/// <summary>
/// Difficulty の外部入力表現（小文字文字列）を enum 定義から導出する。
/// Difficulty にメンバーを追加すれば Validator とメッセージが自動追従する。
/// </summary>
public static class DifficultyNames
{
    public static readonly string[] Allowed =
        [.. Enum.GetNames<Difficulty>().Select(n => n.ToLowerInvariant())];

    public static readonly string AllowedText = string.Join(", ", Allowed);

    public static bool IsValid(string? value) =>
        value is not null && Allowed.Contains(value, StringComparer.OrdinalIgnoreCase);
}
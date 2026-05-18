using System.Text.RegularExpressions;
using RunRoutes.Core.Common.Exceptions;

namespace RunRoutes.Core.Users;

public sealed record Username
{
    public string Value { get; }
    public string Normalized { get; }

    private Username(string value, string normalized)
    {
        Value = value;
        Normalized = normalized;
    }

    public static Username Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ValidationException("ユーザー名は必須です");

        var trimmed = value.Trim();
        if (trimmed.Length < 3 || trimmed.Length > 30)
            throw new ValidationException("ユーザー名は 3〜30 文字で入力してください");
        if (!Pattern.IsMatch(trimmed))
            throw new ValidationException("ユーザー名は英数字・アンダースコア・ハイフンのみ使用できます");

        var normalized = trimmed.ToLowerInvariant();
        if (Reserved.Contains(normalized))
            throw new ValidationException("このユーザー名は使用できません");

        return new Username(trimmed, normalized);
    }

    public bool Equals(Username? other) =>
        other is not null && Normalized == other.Normalized;

    public override int GetHashCode() => Normalized.GetHashCode();

    public override string ToString() => Value;

    private static readonly Regex Pattern =
        new(@"^[a-zA-Z0-9_-]+$", RegexOptions.Compiled);

    private static readonly HashSet<string> Reserved = new(StringComparer.Ordinal)
    {
        "admin", "root", "api", "system", "support", "me",
    };
}
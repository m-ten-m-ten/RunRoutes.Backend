using System.Text.RegularExpressions;
using RunRoutes.Core.Common.Exceptions;

namespace RunRoutes.Core.Users;

public sealed record Username
{
    internal const int MinLength = 3;
    internal const int MaxLength = 30;

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
        if (trimmed.Length < MinLength || trimmed.Length > MaxLength)
            throw new ValidationException($"ユーザー名は {MinLength}〜{MaxLength} 文字で入力してください");
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

    internal static readonly Regex Pattern =
        new(@"^[a-zA-Z0-9_-]+$", RegexOptions.Compiled);

    private static readonly HashSet<string> Reserved = new(StringComparer.Ordinal)
    {
        "admin", "root", "api", "system", "support", "me",
    };

    /// <summary>正規化済み（小文字）の名前が予約語かどうか。Validator からの参照用。</summary>
    internal static bool IsReserved(string normalized) => Reserved.Contains(normalized);
}
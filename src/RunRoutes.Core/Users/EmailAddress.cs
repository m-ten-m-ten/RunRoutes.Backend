using System.Text.RegularExpressions;
using RunRoutes.Core.Common.Exceptions;

namespace RunRoutes.Core.Users;

public sealed record EmailAddress
{
    internal const int MaxLength = 254;

    public string Value { get; }

    private EmailAddress(string value)
    {
        Value = value;
    }

    public static EmailAddress Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ValidationException("メールアドレスは必須です");
        if (value.Length > MaxLength)
            throw new ValidationException($"メールアドレスは {MaxLength} 文字以下にしてください");

        var normalized = value.Trim().ToLowerInvariant();
        if (!Pattern.IsMatch(normalized))
            throw new ValidationException("メールアドレスの形式が正しくありません");

        return new EmailAddress(normalized);
    }

    public override string ToString() => Value;

    internal static readonly Regex Pattern =
        new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
}
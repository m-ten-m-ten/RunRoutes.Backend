using System.Text.RegularExpressions;
using RunRoutes.Core.Common.Exceptions;

public sealed record EmailAddress
{
    public string Value { get; }

    private EmailAddress(string value)
    {
        Value = value;
    }

    public static EmailAddress Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ValidationException("メールアドレスは必須です");
        if (value.Length > 254)
            throw new ValidationException("メールアドレスは 254 文字以下にしてください");

        var normalized = value.Trim().ToLowerInvariant();
        if (!Pattern.IsMatch(normalized))
            throw new ValidationException("メールアドレスの形式が正しくありません");

        return new EmailAddress(normalized);
    }

    public override string ToString() => Value;

    private static readonly Regex Pattern =
        new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
}
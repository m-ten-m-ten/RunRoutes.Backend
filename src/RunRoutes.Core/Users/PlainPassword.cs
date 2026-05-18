using RunRoutes.Core.Common.Exceptions;

namespace RunRoutes.Core.Users;

public sealed record PlainPassword
{
    public string Value { get; }

    private PlainPassword(string value)
    {
        Value = value;
    }

    public static PlainPassword Create(string value)
    {
        if (string.IsNullOrEmpty(value))
            throw new ValidationException("パスワードは必須です");
        if (value.Length < 8)
            throw new ValidationException("パスワードは 8 文字以上で入力してください");
        if (value.Length > 100)
            throw new ValidationException("パスワードは 100 文字以下で入力してください");

        return new PlainPassword(value);
    }

    public override string ToString() => "***";  // ログ漏洩対策
}
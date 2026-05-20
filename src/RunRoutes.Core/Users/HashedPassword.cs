using RunRoutes.Core.Common.Exceptions;

namespace RunRoutes.Core.Users;

public sealed record HashedPassword
{
    public string Value { get; }

    private HashedPassword(string value)
    {
        Value = value;
    }

    public static HashedPassword FromHash(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
            throw new ValidationException("ハッシュ値が空です");
        // BCrypt 形式の簡易チェック($2a$, $2b$, $2y$ で始まる)
        if (!hash.StartsWith("$2"))
            throw new ValidationException("ハッシュ値の形式が正しくありません");
        return new HashedPassword(hash);
    }

    public override string ToString() => "***";  // ログ漏洩対策
}
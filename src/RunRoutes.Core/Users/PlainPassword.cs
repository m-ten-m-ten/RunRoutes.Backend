using RunRoutes.Core.Common.Exceptions;

namespace RunRoutes.Core.Users;

public sealed record PlainPassword
{
    public string Value { get; }

    private PlainPassword(string value)
    {
        Value = value;
    }

    /// <summary>
    /// 新規設定用（登録・パスワード変更後）。8〜100文字のポリシーを強制する。
    /// </summary>
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

    /// <summary>
    /// 照合用（ログイン・現パスワード確認）。必須チェックのみで長さポリシーは課さない。
    /// ポリシー変更前に作られた短いパスワードの既存ユーザーがログインできるようにするため。
    /// </summary>
    public static PlainPassword CreateForVerification(string value)
    {
        if (string.IsNullOrEmpty(value))
            throw new ValidationException("パスワードは必須です");

        return new PlainPassword(value);
    }

    public override string ToString() => "***";  // ログ漏洩対策
}
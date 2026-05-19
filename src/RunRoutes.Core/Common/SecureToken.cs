using System.Buffers.Text;
using System.Security.Cryptography;

namespace RunRoutes.Core.Common;

/// <summary>
/// URL セーフな乱数トークンを生成するヘルパー。
/// メール内リンクのクエリ文字列に乗せても化けないよう Base64Url
/// （'+' '/' '=' を含まない、パディングなし）でエンコードする。
/// </summary>
internal static class SecureToken
{
    public static string Generate(int byteLength = 32) =>
        Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(byteLength));
}

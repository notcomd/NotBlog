using System.Security.Cryptography;

namespace Notcomd.Token.JWT.Security;

/// <summary>
/// 加密安全随机值生成器
/// 
/// 使用 System.Security.Cryptography.RandomNumberGenerator（CSPRNG），
/// 取代低安全性的 System.Random。
/// </summary>
public static class JwtRandom
{
    private const string DefaultCharset =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    /// <summary>
    /// 生成 6 位随机整数（范围 [100000, 999999]）
    /// 使用 RandomNumberGenerator.GetInt32 内置拒绝采样，无取模偏差
    /// </summary>
    public static ValueTask<long> CreateRandomValueTask()
    {
        var value = (long)RandomNumberGenerator.GetInt32(100_000, 1_000_000);
        return new ValueTask<long>(value);
    }

    /// <summary>
    /// 生成 9 位随机字母数字字符串
    /// </summary>
    public static ValueTask<string> CreateRandomStringValueTask()
    {
        var str = RandomNumberGenerator.GetString(DefaultCharset, 9);
        return new ValueTask<string>(str);
    }

    /// <summary>
    /// 生成随机字符串（指定长度和字符集）
    /// 使用 RandomNumberGenerator.GetString 内置拒绝采样，无取模偏差
    /// </summary>
    public static string GenerateRandomString(int length,
        string charset = DefaultCharset)
    {
        return RandomNumberGenerator.GetString(charset, length);
    }

    /// <summary>
    /// 生成安全标记（用于密码重置、邮箱验证等）
    /// 128 位随机，Base64 编码（含 +/=，如需 URL 安全请额外做 Base64Url 编码）
    /// </summary>
    public static ValueTask<string> GenerateSecurityStamp()
    {
        var bytes = new byte[16];
        RandomNumberGenerator.Fill(bytes);
        return new ValueTask<string>(Convert.ToBase64String(bytes));
    }
}

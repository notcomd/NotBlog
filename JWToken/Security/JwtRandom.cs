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
    /// <summary>
    /// 生成 6 位随机整数
    /// </summary>
    public static ValueTask<long> CreateRandomValueTask()
    {
        var bytes = new byte[8];
        RandomNumberGenerator.Fill(bytes);
        // 映射到 [100000, 999999]
        var value = (long)(Math.Abs(BitConverter.ToInt64(bytes)) % 900_000 + 100_000);
        return new ValueTask<long>(value);
    }

    /// <summary>
    /// 生成 9 位随机字母数字字符串
    /// </summary>
    public static ValueTask<string> CreateRandomStringValueTask()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        var bytes = new byte[9];
        RandomNumberGenerator.Fill(bytes);

        Span<char> result = stackalloc char[9];
        for (var i = 0; i < 9; i++)
            result[i] = chars[bytes[i] % chars.Length];

        return new ValueTask<string>(result.ToString());
    }

    /// <summary>
    /// 生成随机字符串（指定长度和字符集）
    /// </summary>
    public static string GenerateRandomString(int length,
        string charset = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789")
    {
        var bytes = new byte[length];
        RandomNumberGenerator.Fill(bytes);

        Span<char> result = stackalloc char[length];
        for (var i = 0; i < length; i++)
            result[i] = charset[bytes[i] % charset.Length];

        return result.ToString();
    }

    /// <summary>
    /// 生成安全标记（用于密码重置、邮箱验证等）
    /// 128 位随机，Base64 编码
    /// </summary>
    public static ValueTask<string> GenerateSecurityStamp()
    {
        var bytes = new byte[16];
        RandomNumberGenerator.Fill(bytes);
        return new ValueTask<string>(Convert.ToBase64String(bytes));
    }
}
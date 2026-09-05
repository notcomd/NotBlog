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

    private const string UpperCharset = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string LowerCharset = "abcdefghijklmnopqrstuvwxyz";
    private const string DigitCharset = "0123456789";
    private const string SpecialCharset = "!@#$%^&*()_+-=[]{}<>?";
    private const string AllPasswordCharset = UpperCharset + LowerCharset + DigitCharset + SpecialCharset;

    /// <summary>
    /// 生成满足复杂度要求的随机密码：保证至少包含大写字母、小写字母、数字、特殊字符各一个，
    /// 其余位从全量字符集中填充，最后做 Fisher-Yates 洗牌打乱相对位置。
    /// 全程使用 CSPRNG（RandomNumberGenerator.GetInt32 / GetString 内置拒绝采样，无取模偏差）。
    /// </summary>
    /// <param name="length">密码长度，默认 12，最小 4</param>
    public static string GenerateComplexPassword(int length = 12)
    {
        if (length < 4)
            throw new ArgumentOutOfRangeException(nameof(length), "密码长度不能小于 4");

        // 前 4 位依次保证包含各类字符
        var chars = new char[length];
        chars[0] = GetRandomChar(UpperCharset);
        chars[1] = GetRandomChar(LowerCharset);
        chars[2] = GetRandomChar(DigitCharset);
        chars[3] = GetRandomChar(SpecialCharset);

        // 其余位从全量字符集随机填充
        for (var i = 4; i < length; i++)
            chars[i] = GetRandomChar(AllPasswordCharset);

        Shuffle(chars);
        return new string(chars);
    }

    /// <summary>
    /// 从指定字符集中取一个随机字符（CSPRNG）。
    /// </summary>
    private static char GetRandomChar(string charset) => charset[RandomNumberGenerator.GetInt32(charset.Length)];

    /// <summary>
    /// Fisher-Yates 原地洗牌（CSPRNG 选择交换下标，确保顺序也随机）。
    /// </summary>
    private static void Shuffle(char[] array)
    {
        for (var i = array.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (array[i], array[j]) = (array[j], array[i]);
        }
    }
}

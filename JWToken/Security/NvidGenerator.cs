using System.Security.Cryptography;
using System.Text;

namespace Notcomd.Token.JWT;

/// <summary>
/// 唯一 ID 生成器
/// </summary>
public static class NvidGenerator
{
    private const string Base62Chars = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

    /// <summary>
    /// 生成 NV 风格 ID（前缀 + 随机字符）
    /// </summary>
    /// <param name="length">随机长度</param>
    /// <param name="prefix">前缀（如 "NV"）</param>
    public static string GenerateNvStyleId(int length, string prefix)
    {
        var randomBytes = new byte[length];
        RandomNumberGenerator.Fill(randomBytes);

        Span<char> result = stackalloc char[length];
        for (var i = 0; i < length; i++)
            result[i] = Base62Chars[randomBytes[i] % Base62Chars.Length];

        return $"{prefix}{result}";
    }

    /// <summary>
    /// 生成 NV 风格 ID（指定长度，随机前缀）
    /// </summary>
    public static string GenerateNvStyleIdWithUuid(int length)
    {
        var randomBytes = new byte[length];
        RandomNumberGenerator.Fill(randomBytes);

        Span<char> result = stackalloc char[length];
        for (var i = 0; i < length; i++)
            result[i] = Base62Chars[randomBytes[i] % Base62Chars.Length];

        return $"BV{result}";
    }

    /// <summary>
    /// 生成 NV 风格 ID（基于 UUID + SHA256）
    /// </summary>
    public static string GenerateNvStyleIdWithUuid()
    {
        var uuid = Guid.NewGuid().ToString("N");
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(uuid));

        Span<char> result = stackalloc char[10];
        for (var i = 0; i < 10; i++)
            result[i] = Base62Chars[hash[i] % Base62Chars.Length];

        return $"NV{result}";
    }

    /// <summary>
    /// 生成 Guid v7（时间排序型 Guid）
    /// </summary>
    public static Guid NewGuidV7()
    {
        var guid = Guid.CreateVersion7();
        return guid;
    }

    /// <summary>
    /// 生成 Guid v7 的字符串表示
    /// </summary>
    public static string NewGuidString() => Guid.CreateVersion7().ToString("N");
}
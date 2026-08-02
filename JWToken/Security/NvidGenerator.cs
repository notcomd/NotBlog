using System.Security.Cryptography;
using System.Text;

namespace Notcomd.Token.JWT.Security;

/// <summary>
/// 唯一 ID 生成器
/// </summary>
public static class NvidGenerator
{
    private const string Base62Chars = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

    // 拒绝采样阈值：62 * (256 / 62) = 248。字节值 248~255 被丢弃重取，
    // 使 [0, 247] 内每个值恰好映射 4 个 Base62 字符，消除 byte(256) % 62 的取模偏差。
    private const int RejectionThreshold = 62 * (256 / 62);

    /// <summary>
    /// 生成 NV 风格 ID（前缀 + 随机字符）
    /// </summary>
    /// <param name="length">随机长度</param>
    /// <param name="prefix">前缀（如 "NV"）</param>
    public static string GenerateNvStyleId(int length, string prefix)
    {
        using var rng = RandomNumberGenerator.Create();

        Span<char> result = stackalloc char[length];
        for (var i = 0; i < length; i++)
            result[i] = GetRandomBase62Char(rng);

        return $"{prefix}{result}";
    }

    /// <summary>
    /// 生成 NV 风格 ID（指定长度，随机前缀）
    /// </summary>
    public static string GenerateNvStyleIdWithUuid(int length)
    {
        using var rng = RandomNumberGenerator.Create();

        Span<char> result = stackalloc char[length];
        for (var i = 0; i < length; i++)
            result[i] = GetRandomBase62Char(rng);

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
        var sourceIndex = 0;
        for (var i = 0; i < 10; i++)
        {
            int value;
            do
            {
                value = hash[sourceIndex++ % hash.Length];
            } while (value >= RejectionThreshold);

            result[i] = Base62Chars[value % Base62Chars.Length];
        }

        return $"NV{result}";
    }

    /// <summary>
    /// 拒绝采样获取单个 Base62 字符：字节值 >= 248 时重取，保证均匀分布。
    /// </summary>
    private static char GetRandomBase62Char(RandomNumberGenerator rng)
    {
        Span<byte> buffer = stackalloc byte[1];
        int value;
        do
        {
            rng.GetBytes(buffer);
            value = buffer[0];
        } while (value >= RejectionThreshold);

        return Base62Chars[value % Base62Chars.Length];
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

using System.Security.Cryptography;
using System.Text;

namespace Notcomd.Token.JWT;

public static class NVIDGenerator
{
    private const string BaseChars = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ"; // 62种字符

    public static string GenerateNvStyleId(int length, string prefix)
    {
        var randomBytes = new byte[length];
        Encoding.UTF8.GetBytes(prefix);
        // 将字节转换为Base62编码的字符串
        var sb = new StringBuilder();
        foreach (var b in randomBytes)
        {
            sb.Append(BaseChars[b % BaseChars.Length]);
        }
        return $"BV{sb.ToString()[..length]}"; // 确保总长度为12（BV+10位）
    }

    public static string GenerateNvStyleIdWithUuid(int lenght)
    {
        var randomBytes = new byte[lenght];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        var sb = new StringBuilder();
        foreach (var b in randomBytes)
        {
            sb.Append(BaseChars[b % BaseChars.Length]);
        }
        return $"BV{sb.ToString()[..lenght]}";
    }

    // 可选：使用更安全的UUID+哈希方式（如需更高唯一性）
    public static string GenerateNvStyleIdWithUuid()
    {
        var uuid = Guid.NewGuid().ToString("N"); // 生成UUID
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(uuid));
        return $"BV{Base62Encode(hash).Substring(0, 10)}";
    }

    // Base62编码辅助函数
    private static string Base62Encode(byte[] bytes)
    {
        var sb = new StringBuilder();
        foreach (var b in bytes)
        {
            sb.Append(BaseChars[b % BaseChars.Length]);
        }
        return sb.ToString();
    }
}
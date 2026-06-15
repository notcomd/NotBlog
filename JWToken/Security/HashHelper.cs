using System.Security.Cryptography;
using System.Text;

namespace Notcomd.Token.JWT.Security;

/// <summary>
/// 哈希校验工具类（SHA256/SHA384/SHA512）
/// 
/// 兼容旧版 API，MD5 默认已升级为 SHA256。
/// </summary>
public static class HashHelper
{
    /// <summary>
    /// 计算字节数组的哈希值
    /// </summary>
    /// <param name="content">字节数组</param>
    /// <param name="algorithmName">算法名称（SHA256/SHA384/SHA512/MD5）</param>
    /// <returns>小写哈希字符串</returns>
    public static string ComputeHash(byte[] content, string algorithmName = "SHA256")
    {
        if (content == null || content.Length == 0)
            return string.Empty;

        var hashBytes = HashData(content, algorithmName);
        return ToHexString(hashBytes);
    }

    /// <summary>
    /// 计算文件的哈希值（流式处理，避免加载大文件到内存）
    /// </summary>
    public static string ComputeFileHash(string filePath, string algorithmName = "SHA256")
    {
        if (!File.Exists(filePath))
            return string.Empty;

        byte[] hashBytes;
        using (var stream = File.OpenRead(filePath))
        {
            using var algorithm = CreateAlgorithm(algorithmName);
            hashBytes = algorithm.ComputeHash(stream);
        }

        return ToHexString(hashBytes);
    }

    /// <summary>
    /// 验证字节数组的哈希值是否匹配
    /// </summary>
    public static bool VerifyHash(byte[] content, string expectedHash, string algorithmName = "SHA256")
    {
        var actualHash = ComputeHash(content, algorithmName);
        return string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 验证文件的哈希值是否匹配
    /// </summary>
    public static bool VerifyFileHash(string filePath, string expectedHash, string algorithmName = "SHA256")
    {
        var actualHash = ComputeFileHash(filePath, algorithmName);
        return string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 使用指定算法计算哈希（返回原始字节）
    /// </summary>
    public static byte[] ComputeHashBytes(byte[] content, string algorithmName = "SHA256")
    {
        return HashData(content, algorithmName);
    }

    private static byte[] HashData(byte[] data, string algorithm)
    {
        return algorithm.ToUpperInvariant() switch
        {
            "SHA256" => SHA256.HashData(data),
            "SHA384" => SHA384.HashData(data),
            "SHA512" => SHA512.HashData(data),
            "MD5" => MD5.HashData(data),
            _ => SHA256.HashData(data)
        };
    }

    private static HashAlgorithm CreateAlgorithm(string algorithm)
    {
        return algorithm.ToUpperInvariant() switch
        {
            "SHA256" => SHA256.Create(),
            "SHA384" => SHA384.Create(),
            "SHA512" => SHA512.Create(),
            "MD5" => MD5.Create(),
            _ => SHA256.Create()
        };
    }

    private static string ToHexString(byte[] bytes)
    {
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}
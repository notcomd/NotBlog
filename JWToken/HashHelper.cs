namespace Notcomd.Token.JWT;

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// 哈希校验工具类（MD5/SHA256）
/// </summary>
public static class HashHelper
{
    /// <summary>
    /// 计算字节数组的哈希值
    /// </summary>
    /// <param name="content">字节数组</param>
    /// <param name="algorithmName">算法名称（MD5/SHA256）</param>
    /// <returns>小写哈希字符串</returns>
    public static string ComputeHash(byte[] content, string algorithmName = "MD5")
    {
        if (content == null || content.Length == 0)
            return string.Empty;

        using (var algorithm = CreateHashAlgorithm(algorithmName))
        {
            byte[] hashBytes = algorithm.ComputeHash(content);
            // 转换为小写十六进制字符串
            var sb = new StringBuilder();
            foreach (byte b in hashBytes)
            {
                sb.Append(b.ToString("x2"));
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// 计算文件的哈希值（避免一次性加载大文件到内存）
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <param name="algorithmName">算法名称</param>
    /// <returns>小写哈希字符串</returns>
    public static string ComputeFileHash(string filePath, string algorithmName = "MD5")
    {
        if (!File.Exists(filePath))
            return string.Empty;

        using (var algorithm = CreateHashAlgorithm(algorithmName))
        using (var stream = File.OpenRead(filePath))
        {
            byte[] hashBytes = algorithm.ComputeHash(stream);
            var sb = new StringBuilder();
            foreach (byte b in hashBytes)
            {
                sb.Append(b.ToString("x2"));
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// 创建哈希算法实例
    /// </summary>
    private static HashAlgorithm CreateHashAlgorithm(string algorithmName)
    {
        return algorithmName.Equals("SHA256", StringComparison.OrdinalIgnoreCase)
            ? (HashAlgorithm)SHA256.Create()
            : MD5.Create();
    }

    /// <summary>
    /// 验证字节数组的哈希值是否匹配
    /// </summary>
    public static bool VerifyHash(byte[] content, string expectedHash, string algorithmName = "MD5")
    {
        string actualHash = ComputeHash(content, algorithmName);
        return actualHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 验证文件的哈希值是否匹配
    /// </summary>
    public static bool VerifyFileHash(string filePath, string expectedHash, string algorithmName = "MD5")
    {
        string actualHash = ComputeFileHash(filePath, algorithmName);
        return actualHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase);
    }
}
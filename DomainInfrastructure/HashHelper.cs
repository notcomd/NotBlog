using System.Security.Cryptography;
using System.Text;

namespace DomainInfrastructure;

/// <summary>
/// 哈希计算辅助类
/// </summary>
public static class HashHelper
{
    private static string ToHexString(byte[] hash)
    {
        var sb = new StringBuilder(hash.Length * 2);
        foreach (var b in hash)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

    /// <summary>计算字符串的 SHA256 哈希值</summary>
    public static string ComputeSha256Hash(string input)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return ToHexString(hash);
    }

    /// <summary>计算流的 SHA256 哈希值</summary>
    public static string ComputeSha256Hash(Stream stream)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(stream);
        return ToHexString(hash);
    }

    /// <summary>计算字符串的 MD5 哈希值</summary>
    public static string ComputeMd5Hash(string input)
    {
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
        return ToHexString(hash);
    }

    /// <summary>计算流的 MD5 哈希值</summary>
    public static string ComputeMd5Hash(Stream stream)
    {
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(stream);
        return ToHexString(hash);
    }
}
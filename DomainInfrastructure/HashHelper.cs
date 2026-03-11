using System.Security.Cryptography;
using System.Text;

namespace Notcomd.DomainCommand;

public static class HashHelper
{
    private static Task<string> ToHashStringAsync(byte[] hash)
    {
        var hashString = new StringBuilder();
        foreach (var itm in hash) hashString.Append(itm.ToString("x2"));
        return Task.FromResult(hashString.ToString());
    }

    /// <summary>
    ///     计算字符串的SHA256哈希值
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static Task<string> ComputeSha254Hash(string str)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(str));
        return ToHashStringAsync(hash);
    }

    /// <summary>
    ///     计算流的MD5哈希值
    /// </summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    public static Task<string> ComputeSha254Hash(Stream stream)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(stream);
        return ToHashStringAsync(hash);
    }


    /// <summary>
    ///     计算字符串的MD5哈希值
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static Task<string> ComputeMd5Hash(string str)
    {
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(str));
        return ToHashStringAsync(hash);
    }

    /// <summary>
    ///     计算流的MD5哈希值
    /// </summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    public static Task<string> ComputeMd5Hash(Stream stream)
    {
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(stream);
        return ToHashStringAsync(hash);
    }
}
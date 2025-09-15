using System.Security.Cryptography;
using System.Text;

namespace Notcomd.Token.JWT;

public sealed class HashHelper
{
    public static string CreateHash256Async(string hashString, byte[] salt)
    {
        var data = Encoding.UTF8.GetBytes(hashString);
        using var myHash = new Rfc2898DeriveBytes(hashString, salt, 2000, HashAlgorithmName.SHA384);
        var hash = myHash.GetBytes(64);
        return Convert.ToBase64String(hash);
    }

    public static byte[] GenerateSaltValue(int size = 64)
    {
        return RandomNumberGenerator.GetBytes(size);
    }

    public static string GenerateToString(byte[] bytes)
    {
        return Convert.ToBase64String(bytes) switch
        {
            string toString => toString,
            _ => throw new InvalidOperationException("Not Byte to string")
        };
    }

    public static byte[] ConvertStringToBytes(string byteString)
    {
        return Convert.FromBase64String(byteString) switch
        {
            byte[] toBytes => toBytes,
            _ => throw new InvalidOperationException("Not String to Bytes")
        };
    }

    public static string GenerateSecurityStamp()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes) switch
        {
            string securityStamp => securityStamp,
            _ => throw new InvalidOperationException("Failed to generate security stamp.")
        };
    }

    public static bool VerifyPasswordValueTask(string password, string hash, byte[] sart)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash) || sart is null || sart.Length == 0)
        {
            return false;
        }
        var hashStr = CreateHash256Async(password, sart);
        return hashStr == hash;
    }

    public static ValueTask<string> HexGenerateHaxCode(string hexStr, int length)
    {
        var bytes = Encoding.UTF8.GetBytes(hexStr);
        using var myBash = new Rfc2898DeriveBytes(hexStr, bytes, 2000, HashAlgorithmName.SHA384);
        var hxCode = myBash.GetBytes(length);
        return new ValueTask<string>(Convert.ToBase64String(hxCode));
    }

    public static ValueTask<string> ComputeWithStreamHashAsync(Stream stream)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(stream);
        var hashString = new StringBuilder();
        foreach (var itm in hash)
        {
            hashString.Append(itm.ToString("x2"));
        }
        return new ValueTask<string>(hashString.ToString());
    }

}
using System.Security.Cryptography;
using System.Text;

namespace Notcomd.Token.JWT;

public sealed class HashHelper
{
    public static ValueTask<string> CreateHash256Async(string hashString, byte[] salt)
    {
        var data = Encoding.UTF8.GetBytes(hashString);
        using var myHash = new Rfc2898DeriveBytes(hashString, salt, 2000, HashAlgorithmName.SHA384);
        var hash = myHash.GetBytes(64);
        return new ValueTask<string>(Convert.ToBase64String(hash));
    }

    public static ValueTask<byte[]> GenerateSaltValueTask()
    {
        return new ValueTask<byte[]>(RandomNumberGenerator.GetBytes(64));
    }


    public static ValueTask<string> GenerateSecurityStamp()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes) switch
        {
            string securityStamp => new ValueTask<string>(securityStamp),
            _ => throw new InvalidOperationException("Failed to generate security stamp.")
        };
    }


    public async static ValueTask<bool> VerifyPasswordValueTask(string password, string hash, byte[] sart)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash) || sart is null || sart.Length == 0)
        {
            return false;
        }
        var hashStr = await CreateHash256Async(password, sart);
        return hashStr== hash;
    }

    public static ValueTask<string> HexGenerateHaxCode(string hexStr, int length)
    {
        var bytes = Encoding.UTF8.GetBytes(hexStr);
        using var myBash = new Rfc2898DeriveBytes(hexStr, bytes, 2000, HashAlgorithmName.SHA384);
        var hxCode = myBash.GetBytes(length);
        return new ValueTask<string>(Convert.ToBase64String(hxCode));
    }
}
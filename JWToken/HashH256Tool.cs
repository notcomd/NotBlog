using System.Security.Cryptography;
using System.Text;

namespace Notcomd.Token.JWT;

public sealed class HashH256Tool
{
    public static ValueTask<string> CreateHash256Async(string hashString, byte[] salt)
    {
        var data = Encoding.UTF8.GetBytes(hashString);
        using var myHash = new Rfc2898DeriveBytes(hashString, salt, 2000, HashAlgorithmName.SHA384);
        var hash = myHash.GetBytes(64);
        return new ValueTask<string>(Convert.ToBase64String(hash));
    }

    public static ValueTask<byte[]> GenerateSValueTask()
    {
        return new ValueTask<byte[]>(RandomNumberGenerator.GetBytes(64));
    }

    public static ValueTask<bool> VerifyPasswordValueTask(string password, string hash, byte[] sart)
    {
        return new ValueTask<bool>(CreateHash256Async(password, sart).Result == hash);
    }

    public static ValueTask<string> HexGenerateHaxCode(string hexStr, int length)
    {
        var bytes = Encoding.UTF8.GetBytes(hexStr);
        using var myBash = new Rfc2898DeriveBytes(hexStr, bytes, 2000, HashAlgorithmName.SHA384);
        var hxCode = myBash.GetBytes(length);
        return new ValueTask<string>(Convert.ToBase64String(hxCode));
    }
}
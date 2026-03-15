using System.Security.Cryptography;
using System.Text;

namespace Notcomd.Token.JWT;

public static class HashH256Tool
{
    public static ValueTask<string> CreateHash256Async(string hashString, byte[] salt)
    {
        var data = Encoding.UTF8.GetBytes(hashString);
        var myHash = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(hashString), salt, 2000, HashAlgorithmName.SHA384, 64);
        return new ValueTask<string>(Convert.ToBase64String(myHash));
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
        var myBash = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(hexStr),bytes, 2000, HashAlgorithmName.SHA384, 64);
        return new ValueTask<string>(Convert.ToBase64String(myBash));
    }
}
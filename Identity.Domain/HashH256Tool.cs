using System.Security.Cryptography;
using System.Text;

namespace Identity.Domain;

public sealed class HashH256Tool
{
    public static ValueTask<string> CreateHash256Async(string hashString, byte[] salt)
    {
        var data = Encoding.UTF8.GetBytes(hashString);
        using var myHash = new Rfc2898DeriveBytes(hashString, salt, 2000, HashAlgorithmName.SHA384);
        var hash = myHash.GetBytes(64);
        return new ValueTask<string>(Convert.ToBase64String(hash));
    }

    public static ValueTask<byte[]> GenerateSValueTask() => new ValueTask<byte[]>(RandomNumberGenerator.GetBytes(64));

    public static ValueTask<bool> VerifyPasswordValueTask(string password, string hash, byte[] sart) => new ValueTask<bool>(CreateHash256Async(password, sart).Result == hash);
}
using System.Security.Cryptography;
using System.Text;

namespace Identity.Domain;

public sealed class HashH256Tool
{
    public static ValueTask<string> CreateHash256Async(string hashString)
    {
        var data = Encoding.UTF8.GetBytes(hashString);
        using var myHash = SHA512.Create();
        var hash = myHash.ComputeHash(data);
        return new ValueTask<string>(Convert.ToBase64String(hash));
    }
}
using System.Security.Cryptography;
using System.Text;

namespace Markdown.Domain;

public sealed class HashH256Tool
{
    
    public static ValueTask<string> CreateHash256Async(string HashString)
    {
        var data = Encoding.UTF8.GetBytes(HashString);
        using (var myHash = SHA3_512.Create())
        {
            var Hash = Convert.ToBase64String(myHash.ComputeHash(data));
            return new ValueTask<string>(Hash);
        }
    }
    
}
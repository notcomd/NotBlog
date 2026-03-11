using System.Security.Cryptography;

namespace Notcomd.Token.JWT;

public static class JwtRandom
{
    /// <summary>
    ///     创建随机数
    /// </summary>
    /// <returns></returns>
    public static ValueTask<long> CreateRandomValueTask()
    {
        var random = new Random().NextInt64(100000, 999999);
        return new ValueTask<long>(random);
    }

    /// <summary>
    ///     创建随机字符串
    /// </summary>
    /// <returns></returns>
    public static ValueTask<string> CreateRandomStringValueTask()
    {
        var codeString = string.Empty;
        while (codeString.Length <= 8)
        {
            var random = new Random().Next(48, 122);
            if (random is >= 48 and <= 57 || random is >= 65 and <= 90 || random is >= 97 and <= 122)
                codeString += Convert.ToChar(random);
        }

        return new ValueTask<string>(codeString);
    }


    public static ValueTask<string> GenerateSecurityStamp()
    {
        var bytes = new byte[16];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes) switch
        {
            string securityStamp => new ValueTask<string>(securityStamp),
            _ => throw new InvalidOperationException("Failed to generate security stamp.")
        };
    }
}
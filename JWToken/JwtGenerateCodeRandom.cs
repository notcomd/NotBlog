using System.Security.Cryptography;
using System.Text;

namespace Notcomd.Token.JWT;

public static class JwtGenerateCodeRandom
{

    private static readonly Random random = new Random();

    private static readonly object _lock = new object();
    /// <summary>
    /// 创建随机数
    /// </summary>
    /// <returns></returns>
    public static ValueTask<string> CreateRandomValueTask(in int length)
    {

        var GenerateCode = new StringBuilder(length);

        for (int item = 0; item < length; item++)
        {
            lock (_lock)
            {
                GenerateCode.Append(random.Next(0, 10));
            }
        }
        return new ValueTask<string>(GenerateCode.ToString());
    }

    /// <summary>
    /// 创建随机字符串
    /// </summary>
    /// <returns></returns>
    public static ValueTask<string> CreateRandomStringValueTask(in int length)
    {
        var GenerateCode = new StringBuilder(length);
        for (int item = 0; item < length; item++)
        {
            lock (_lock)
            {
                var rand = random.Next(48, 122);
                if (rand is >= 48 and <= 57 || rand is >= 65 and <= 90 || rand is >= 97 and <= 122)
                {
                    GenerateCode.Append(Convert.ToChar(random));
                }
            }

        }
        return new ValueTask<string>(GenerateCode.ToString());
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
using System.Security.Cryptography;
using System.Text;

namespace Notcomd.Token.JWT.Security;

public static class PasswordHasher
{
    private const int DefaultIterations = 100_000;
    private const int SaltSize = 64;
    private const int HashSize = 64;

    /// <summary>
    /// 使用随机 salt 哈希密码
    /// </summary>
    /// <returns>(Base64-Hash, Base64-Salt)</returns>
    public static (string Hash, string Salt) Hash(string password, int iterations = DefaultIterations)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password), salt, iterations,
            HashAlgorithmName.SHA256, HashSize);

        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }

    /// <summary>
    /// 验证密码
    /// </summary>
    /// <param name="password">明文密码</param>
    /// <param name="hash">Base64 编码的哈希</param>
    /// <param name="salt">Base64 编码的盐值</param>
    /// <param name="iterations">迭代次数（默认 100,000）</param>
    public static bool Verify(string password, string hash, string salt, int iterations = DefaultIterations)
    {
        var saltBytes = Convert.FromBase64String(salt);
        var computedHash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password), saltBytes, iterations,
            HashAlgorithmName.SHA256, HashSize);

        return CryptographicOperations.FixedTimeEquals(
            computedHash, Convert.FromBase64String(hash));
    }
}
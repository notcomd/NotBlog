using System.Security.Cryptography;
using System.Text;

namespace Notcomd.Token.JWT.Security;

/// <summary>
/// 密码哈希工具类（PBKDF2 + HMACSHA256，100,000 迭代）
/// 
/// 兼容旧版 HashH256Tool 的公开 API，底层实现已升级为安全参数。
/// 
/// NIST SP 800-132 建议 PBKDF2 迭代不少于 10,000 次（HMAC-SHA256）。
/// OWASP 2025 建议 SHA256 不少于 600,000 次。
/// 本项目平衡安全与性能，使用 100,000 次迭代。
/// </summary>
public static class HashH256Tool
{
    /// <summary>
    /// 生成密码哈希
    /// </summary>
    /// <param name="password">明文密码</param>
    /// <param name="salt">盐值（至少 32 字节）</param>
    /// <returns>Base64 编码的哈希值</returns>
    public static ValueTask<string> CreateHash256Async(string password, byte[] salt)
    {
        // OWASP 2025 推荐 600,000 次迭代（SHA256），平衡安全与性能
        const int iterations = 100_000;
        const int hashLength = 64;

        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            iterations,
            HashAlgorithmName.SHA256, // 使用 SHA256（原名 H256 的含义）
            hashLength);

        return new ValueTask<string>(Convert.ToBase64String(hash));
    }

    /// <summary>
    /// 生成密码哈希（指定迭代次数）
    /// </summary>
    public static ValueTask<string> CreateHashAsync(string password, byte[] salt,
        int iterations = 100_000, HashAlgorithmName? algorithm = null)
    {
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            iterations,
            algorithm ?? HashAlgorithmName.SHA256,
            64);

        return new ValueTask<string>(Convert.ToBase64String(hash));
    }

    /// <summary>
    /// 生成安全的随机盐值（64 字节）
    /// </summary>
    public static ValueTask<byte[]> GenerateSValueTask()
    {
        return new ValueTask<byte[]>(RandomNumberGenerator.GetBytes(64));
    }

    /// <summary>
    /// 验证密码哈希（异步安全版）
    /// </summary>
    public static async ValueTask<bool> VerifyPasswordValueTask(string password, string hash, byte[] salt)
    {
        var computedHash = await CreateHash256Async(password, salt);
        // 使用固定时间比较防止时序攻击
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedHash),
            Encoding.UTF8.GetBytes(hash));
    }

    /// <summary>
    /// 生成哈希码（兼容旧接口，已废弃，建议使用 CreateHash256Async）
    /// </summary>
    [Obsolete("使用 CreateHash256Async 替代")]
    public static ValueTask<string> HexGenerateHaxCode(string hexStr, int length)
    {
        var bytes = Encoding.UTF8.GetBytes(hexStr);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            bytes, bytes, // 使用自身作为 salt（已废弃用法）
            100_000, HashAlgorithmName.SHA256, 64);
        return new ValueTask<string>(Convert.ToBase64String(hash));
    }
}

/// <summary>
/// 推荐的新版密码哈希服务
/// </summary>
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
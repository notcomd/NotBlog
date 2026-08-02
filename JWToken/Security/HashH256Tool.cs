using System.Security.Cryptography;
using System.Text;

namespace Notcomd.Token.JWT.Security;

/// <summary>
/// 密码哈希工具类（PBKDF2 + HMACSHA256，600,000 迭代）
///
/// 兼容旧版 HashH256Tool 的公开 API，底层实现已升级为安全参数。
///
/// NIST SP 800-132 建议 PBKDF2 迭代不少于 10,000 次（HMAC-SHA256）。
/// OWASP 2025 建议 SHA256 不少于 600,000 次。
/// 本项目采用 600,000 次迭代（S-13），同时保留旧 100,000 次哈希的验证兼容，
/// 登录校验通过后由上层（User.VerifyByPasswordAsync）触发重哈希升级。
/// </summary>
public static class HashH256Tool
{
    /// <summary>
    /// 当前迭代次数（OWASP 2025 推荐 600,000 次）
    /// </summary>
    public const int CurrentIterations = 600_000;

    /// <summary>
    /// 旧版迭代次数（兼容升级前已存储的哈希）
    /// </summary>
    public const int LegacyIterations = 100_000;

    /// <summary>
    /// 生成密码哈希
    /// </summary>
    /// <param name="password">明文密码</param>
    /// <param name="salt">盐值（至少 32 字节）</param>
    /// <returns>Base64 编码的哈希值</returns>
    public static ValueTask<string> CreateHash256Async(string password, byte[] salt)
    {
        // OWASP 2025 推荐 600,000 次迭代（SHA256）
        const int iterations = CurrentIterations;
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
        int iterations = CurrentIterations, HashAlgorithmName? algorithm = null)
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
    /// 验证密码哈希（异步安全版）。
    /// 优先按当前迭代（600K）验证，失败时按旧迭代（100K）验证，兼容升级前的旧哈希。
    /// </summary>
    public static async ValueTask<bool> VerifyPasswordValueTask(string password, string hash, byte[] salt)
    {
        var computedHash = await CreateHash256Async(password, salt);
        // 使用固定时间比较防止时序攻击
        if (CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(computedHash),
                Encoding.UTF8.GetBytes(hash)))
        {
            return true;
        }

        // 兼容旧版 100K 迭代的哈希（S-13 升级路径）
        var legacyHash = await CreateHashAsync(password, salt, LegacyIterations);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(legacyHash),
            Encoding.UTF8.GetBytes(hash));
    }

    /// <summary>
    /// 验证密码哈希并返回是否需要升级重哈希（S-13）。
    /// </summary>
    /// <returns>Valid=密码是否正确；NeedsRehash=哈希为旧迭代（100K），应在登录成功后用新迭代重哈希保存</returns>
    public static async ValueTask<(bool Valid, bool NeedsRehash)> VerifyPasswordWithUpgradeAsync(
        string password, string hash, byte[] salt)
    {
        var computedHash = await CreateHash256Async(password, salt);
        if (CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(computedHash),
                Encoding.UTF8.GetBytes(hash)))
        {
            return (true, false);
        }

        // 旧版 100K 迭代哈希：验证通过但需要升级
        var legacyHash = await CreateHashAsync(password, salt, LegacyIterations);
        if (CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(legacyHash),
                Encoding.UTF8.GetBytes(hash)))
        {
            return (true, true);
        }

        return (false, false);
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
            LegacyIterations, HashAlgorithmName.SHA256, 64);
        return new ValueTask<string>(Convert.ToBase64String(hash));
    }
}

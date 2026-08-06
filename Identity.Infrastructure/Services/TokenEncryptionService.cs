using System.Security.Cryptography;
using System.Text;

namespace Identity.Infrastructure.Services;

/// <summary>
/// 基于 AES-256-GCM 的令牌加密实现（认证加密，防篡改）。
/// 密钥来源：环境变量 OAUTH_TOKEN_ENCRYPTION_KEY 优先，其次配置 OAuthOptions:TokenEncryptionKey；
/// 均为 Base64 编码的 32 字节密钥。密钥缺失时抛出清晰异常（fail-fast，禁止明文落库）。
/// 密文格式: Base64( nonce(12) || ciphertext || tag(16) )
/// </summary>
public class TokenEncryptionService(IConfiguration configuration) : ITokenEncryptionService
{
    private const string EnvKey = "OAUTH_TOKEN_ENCRYPTION_KEY";
    private const string ConfigKey = "OAuthOptions:TokenEncryptionKey";

    private readonly byte[] _key = LoadKey(configuration);

    private static byte[] LoadKey(IConfiguration configuration)
    {
        var base64 = Environment.GetEnvironmentVariable(EnvKey);
        if (string.IsNullOrWhiteSpace(base64))
            base64 = configuration[ConfigKey];

        if (string.IsNullOrWhiteSpace(base64))
            throw new InvalidOperationException(
                "未配置外部登录令牌加密密钥：请设置环境变量 OAUTH_TOKEN_ENCRYPTION_KEY（Base64 编码的 32 字节密钥）。");

        byte[] key;
        try
        {
            key = Convert.FromBase64String(base64);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException("令牌加密密钥必须为 Base64 编码");
        }

        if (key.Length != 32)
            throw new InvalidOperationException($"令牌加密密钥必须为 32 字节（AES-256），当前为 {key.Length} 字节");

        return key;
    }

    public string Encrypt(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
            return plaintext;

        var nonce = RandomNumberGenerator.GetBytes(12);
        var plainBytes = Encoding.UTF8.GetBytes(plaintext);
        var ciphertext = new byte[plainBytes.Length];
        var tag = new byte[16];

        using var aes = new AesGcm(_key, 16);
        aes.Encrypt(nonce, plainBytes, ciphertext, tag);

        var result = new byte[nonce.Length + ciphertext.Length + tag.Length];
        nonce.CopyTo(result, 0);
        ciphertext.CopyTo(result, nonce.Length);
        tag.CopyTo(result, nonce.Length + ciphertext.Length);

        return Convert.ToBase64String(result);
    }

    public string Decrypt(string encrypted)
    {
        if (string.IsNullOrEmpty(encrypted))
            return encrypted;

        var data = Convert.FromBase64String(encrypted);
        if (data.Length < 12 + 16)
            throw new InvalidOperationException("密文格式无效");

        var nonce = data[..12];
        var tag = data[^16..];
        var ciphertext = data[12..^16];
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(_key, 16);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }
}

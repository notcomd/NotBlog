namespace Identity.Infrastructure.Services;

/// <summary>
/// 外部登录令牌加解密服务（AES-256-GCM 认证加密）
/// </summary>
public interface ITokenEncryptionService
{
    /// <summary>加密明文，返回 Base64 密文（nonce || ciphertext || tag）</summary>
    string Encrypt(string plaintext);

    /// <summary>解密密文；密钥错误或数据被篡改时抛出 CryptographicException</summary>
    string Decrypt(string encrypted);
}

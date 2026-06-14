namespace Notcomd.Token.JWT;

/// <summary>
/// JWT 配置选项（兼容现有 appsettings.json 字段名）
/// </summary>
public class JwtOptions
{
    // 内部字段：用 Audience 属性兼容 Audiencs 拼写
    private string? _audience;

    /// <summary>签发者</summary>
    public string Issuer { get; set; } = null!;

    /// <summary>接收者（兼容旧配置拼写，建议使用 Audience）</summary>
    public string Audiencs
    {
        get => _audience ?? Issuer;
        set => _audience = value;
    }

    /// <summary>接收者（推荐使用此属性）</summary>
    public string? Audience
    {
        get => _audience;
        set => _audience = value;
    }

    /// <summary>签名密钥（对称算法为共享密钥，非对称为私钥路径）</summary>
    public string PrivateKey { get; set; } = null!;

    /// <summary>过期时间（秒）</summary>
    public int ExpirSeconds { get; set; } = 3600;

    /// <summary>Refresh Token 过期时间（秒，默认 7 天）</summary>
    public int RefreshTokenExpirSeconds { get; set; } = 604800;

    /// <summary>签名算法（默认 HS256）</summary>
    public string Algorithm { get; set; } = SecurityAlgorithms.HmacSha256;

    /// <summary>是否验证签发者</summary>
    public bool ValidateIssuer { get; set; } = true;

    /// <summary>是否验证接收者</summary>
    public bool ValidateAudience { get; set; } = true;

    /// <summary>是否验证有效期</summary>
    public bool ValidateLifetime { get; set; } = true;

    /// <summary>是否验证签名密钥</summary>
    public bool ValidateIssuerSigningKey { get; set; } = true;

    /// <summary>时钟偏差（秒，默认 5 分钟）</summary>
    public int ClockSkewSeconds { get; set; } = 300;

    /// <summary>黑名单最大容量</summary>
    public int BlacklistCapacity { get; set; } = 10000;
}

/// <summary>
/// 支持的签名算法常量
/// </summary>
public static class SecurityAlgorithms
{
    public const string HmacSha256 = "HS256";
    public const string HmacSha384 = "HS384";
    public const string HmacSha512 = "HS512";
    public const string RsaSha256 = "RS256";
    public const string RsaSha384 = "RS384";
    public const string RsaSha512 = "RS512";
    public const string EcdsaSha256 = "ES256";
    public const string EcdsaSha384 = "ES384";
    public const string EcdsaSha512 = "ES512";
}
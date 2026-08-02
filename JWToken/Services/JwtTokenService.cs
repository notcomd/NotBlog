using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Notcomd.Token.JWT.Core;
using SecurityAlgorithms = Notcomd.Token.JWT.Core.SecurityAlgorithms;

namespace Notcomd.Token.JWT;

/// <summary>
/// JWT Token 服务核心实现
/// 
/// 支持算法: HS256 / HS384 / HS512
/// 特性: Token 生成 / 验证 / 刷新 / 吊销 + 黑名单
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    /// <summary>
    /// 黑名单（进程内实现）。
    /// 局限：多实例部署时各实例黑名单不共享，如需全局生效应替换为 Redis 等共享存储。
    /// 已通过 JwtBearer 的 OnTokenValidated 事件接入认证管线（见 AuthenticationExtensions）。
    /// </summary>
    private readonly ConcurrentDictionary<string, DateTimeOffset> _blacklist = new();
    private readonly IOptionsSnapshot<JwtOptions> _options;

    public JwtTokenService(IOptionsSnapshot<JwtOptions> options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    // ── 生成 Token ──

    /// <summary>
    /// 同步生成 Token（兼容旧接口）
    /// </summary>
    public string BuilderTokenAsync(IEnumerable<Claim> claims, JwtOptions configuration)
    {
        var result = BuildTokenInternal(claims, configuration);
        return new JwtSecurityTokenHandler().WriteToken(result);
    }

    /// <summary>
    /// 生成 Token（异步，含 RefreshToken）
    /// RefreshToken 为自包含签名串：Base64Url(Claims+过期时间).Base64Url(HMAC 签名)，
    /// 与 RefreshTokenAsync 的校验逻辑一一对应。
    /// </summary>
    public Task<TokenResult> BuildTokenAsync(IEnumerable<Claim> claims, JwtOptions configuration)
    {
        var now = DateTime.UtcNow;
        var expiry = now.AddSeconds(configuration.ExpireSeconds);

        var token = BuildTokenInternal(claims, configuration);
        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        var refreshToken = GenerateRefreshToken(claims, configuration, now);
        var claimList = claims.Select(c => new ClaimInfo(c.Type, c.Value)).ToList();

        return Task.FromResult(new TokenResult
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            TokenType = "Bearer",
            ExpiresAt = new DateTimeOffset(expiry, TimeSpan.Zero),
            Claims = claimList
        });
    }

    // ── 验证 Token ──

    public async Task<TokenValidationResult> JwtSecurityTokenHandlerAsync(
        [Required(ErrorMessage = "privateKey is null!")]
        string privateKey,
        string authorizationString)
    {
        var validationParameters = CreateValidationParameters(privateKey);
        var handler = new JwtSecurityTokenHandler();
        return await handler.ValidateTokenAsync(authorizationString, validationParameters);
    }

    public async Task<TokenValidationResult> JwtSecurityTokenHandlerAsync(string authorizationString)
    {
        var privateKey = _options.Value.PrivateKey;
        return await JwtSecurityTokenHandlerAsync(privateKey, authorizationString);
    }

    public ClaimsPrincipal? ValidateToken(string token, JwtOptions configuration)
    {
        try
        {
            // 检查黑名单
            if (IsRevoked(token))
                return null;

            var validationParameters = CreateValidationParameters(configuration.PrivateKey, configuration);
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, validationParameters, out _);
            return principal;
        }
        catch
        {
            return null;
        }
    }

    // ── 刷新 Token ──

    /// <summary>
    /// 刷新 Token：与生成逻辑对称。
    /// 1. 校验格式（payload.signature 两段）；
    /// 2. 校验 HMAC 签名；
    /// 3. 校验过期时间（生成时内嵌 RefreshTokenExpireSeconds 对应的绝对过期时刻）；
    /// 4. 单次使用：刷新成功后旧 RefreshToken 加入黑名单，二次使用即被拒绝。
    /// </summary>
    public async Task<TokenResult> RefreshTokenAsync(string refreshToken, JwtOptions configuration)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new SecurityTokenException("RefreshToken 不能为空");

        // 单次使用：已被使用（或吊销）的 RefreshToken 直接拒绝
        if (IsRevoked(refreshToken))
            throw new SecurityTokenException("RefreshToken 已被使用或吊销");

        var parts = refreshToken.Split('.');
        if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
            throw new SecurityTokenException("无效的 RefreshToken 格式");

        var payload = parts[0];
        var signature = parts[1];

        var expectedSignature = ComputeHmac(payload, configuration.PrivateKey);
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(signature),
                Encoding.UTF8.GetBytes(expectedSignature)))
        {
            throw new SecurityTokenException("RefreshToken 签名无效");
        }

        // 解析 payload 中的 Claims 与过期时间
        RefreshTokenPayload? data;
        try
        {
            var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(payload));
            data = JsonSerializer.Deserialize<RefreshTokenPayload>(payloadJson);
        }
        catch
        {
            throw new SecurityTokenException("RefreshToken 数据无效");
        }

        if (data is null || data.Claims is null || data.Claims.Length == 0)
            throw new SecurityTokenException("RefreshToken 数据无效");

        // 过期校验：以生成时内嵌的绝对过期时刻为准（与 RefreshTokenExpireSeconds 一致）
        var nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (data.ExpiresAt <= 0 || data.ExpiresAt < nowUnix)
            throw new SecurityTokenException("RefreshToken 已过期");

        // 单次使用：作废旧 RefreshToken（黑名单保留至其过期时刻）
        await RevokeTokenAsync(refreshToken, DateTimeOffset.FromUnixTimeSeconds(data.ExpiresAt));

        var claims = data.Claims.Select(c => new Claim(c.Type, c.Value)).ToArray();
        return await BuildTokenAsync(claims, configuration);
    }

    // ── 吊销 Token ──

    public Task RevokeTokenAsync(string token, DateTimeOffset? expiresAt = null)
    {
        var capacity = _options.Value.BlacklistCapacity;
        // LRU 淘汰：超过容量时清空一半
        if (_blacklist.Count >= capacity)
        {
            var keysToRemove = _blacklist.OrderBy(kv => kv.Value).Take(capacity / 2).Select(kv => kv.Key);
            foreach (var key in keysToRemove)
                _blacklist.TryRemove(key, out _);
        }

        _blacklist[token] = expiresAt ?? DateTimeOffset.UtcNow.AddHours(24);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 检查 token 是否已进入黑名单（被吊销或已使用的 RefreshToken）。
    /// 注意：进程内实现，多实例部署需替换为 Redis 共享黑名单。
    /// </summary>
    public bool IsRevoked(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return true;

        return _blacklist.ContainsKey(token);
    }

    private JwtSecurityToken BuildTokenInternal(IEnumerable<Claim> claims, JwtOptions config)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config.PrivateKey));
        var algorithm = config.Algorithm switch
        {
            "HS384" => SecurityAlgorithms.HmacSha384,
            "HS512" => SecurityAlgorithms.HmacSha512,
            _ => SecurityAlgorithms.HmacSha256
        };

        var signingCredentials = new SigningCredentials(key, algorithm);
        var now = DateTime.UtcNow;
        var audience = ResolveAudience(config);

        return new JwtSecurityToken(
            issuer: config.Issuer,
            audience: audience,
            claims: claims,
            notBefore: now,
            expires: now.AddSeconds(config.ExpireSeconds),
            signingCredentials: signingCredentials);
    }

    // ── 内部辅助 ──

    private TokenValidationParameters CreateValidationParameters(string privateKey, JwtOptions? config = null)
    {
        config ??= _options.Value;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(privateKey));
        var audience = ResolveAudience(config);

        return new TokenValidationParameters
        {
            ValidateIssuer = config.ValidateIssuer,
            ValidateAudience = config.ValidateAudience,
            ValidateLifetime = config.ValidateLifetime,
            ValidateIssuerSigningKey = config.ValidateIssuerSigningKey,
            ValidIssuer = config.Issuer,
            ValidAudience = audience,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.FromSeconds(config.ClockSkewSeconds)
        };
    }

    private static string ResolveAudience(JwtOptions config) =>
        config.Audiences ?? config.Issuer;

    /// <summary>
    /// 生成 RefreshToken：Base64Url(Claims + 过期时间).Base64Url(HMAC 签名)。
    /// 签名密钥与 AccessToken 同源（PrivateKey），防止伪造。
    /// </summary>
    private static string GenerateRefreshToken(IEnumerable<Claim> claims, JwtOptions configuration, DateTime now)
    {
        var payload = new RefreshTokenPayload
        {
            ExpiresAt = new DateTimeOffset(now, TimeSpan.Zero)
                .AddSeconds(configuration.RefreshTokenExpireSeconds)
                .ToUnixTimeSeconds(),
            Claims = claims.Select(c => new ClaimData(c.Type, c.Value)).ToArray()
        };

        var payloadB64 = Base64UrlEncode(JsonSerializer.Serialize(payload));
        var signature = ComputeHmac(payloadB64, configuration.PrivateKey);
        return payloadB64 + "." + signature;
    }

    private static string ComputeHmac(string payload, string key)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var hash = HMACSHA256.HashData(keyBytes, payloadBytes);
        return Convert.ToBase64String(hash)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    private static string Base64UrlEncode(string value)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static byte[] Base64UrlDecode(string value)
    {
        var s = value.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
            case 1: throw new FormatException("Invalid base64url string");
        }

        return Convert.FromBase64String(s);
    }

    /// <summary>
    /// RefreshToken 载荷：内嵌绝对过期时刻 + 原始 Claims（刷新时重建 AccessToken 使用）
    /// </summary>
    private sealed class RefreshTokenPayload
    {
        public long ExpiresAt { get; set; }

        public ClaimData[] Claims { get; set; } = Array.Empty<ClaimData>();
    }

    private record ClaimData(string Type, string Value);
}

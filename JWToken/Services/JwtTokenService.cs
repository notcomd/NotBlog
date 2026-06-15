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
    /// </summary>
    public Task<TokenResult> BuildTokenAsync(IEnumerable<Claim> claims, JwtOptions configuration)
    {
        var now = DateTime.UtcNow;
        var expiry = now.AddSeconds(configuration.ExpirSeconds);

        var token = BuildTokenInternal(claims, configuration);
        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        var refreshToken = GenerateRefreshToken();
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
            if (_blacklist.TryGetValue(token, out _))
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

    public Task<TokenResult> RefreshTokenAsync(string refreshToken, JwtOptions configuration)
    {
        // 验证 RefreshToken: 简单的 HMAC 验证
        var parts = refreshToken.Split('.');
        if (parts.Length != 2)
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

        // 解析 payload 中的 Claims
        var claimsJson = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
        var claims = JsonSerializer.Deserialize<ClaimData[]>(claimsJson)
                         ?.Select(c => new Claim(c.Type, c.Value))
                     ?? Enumerable.Empty<Claim>();

        return BuildTokenAsync(claims, configuration);
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
            expires: now.AddSeconds(config.ExpirSeconds),
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
        config.Audience ?? config.Issuer;

    private static string GenerateRefreshToken()
    {
        var payloadBytes = new byte[32];
        RandomNumberGenerator.Fill(payloadBytes);
        return Convert.ToBase64String(payloadBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
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

    private record ClaimData(string Type, string Value);
}
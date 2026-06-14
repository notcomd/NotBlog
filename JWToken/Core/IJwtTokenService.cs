using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace Notcomd.Token.JWT;

/// <summary>
/// JWT Token 服务接口
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// 生成 JWT Token
    /// </summary>
    /// <param name="claims">用户声明（Claims）</param>
    /// <param name="configuration">JWT 配置</param>
    /// <returns>生成的 Token 字符串</returns>
    string BuilderTokenAsync(IEnumerable<Claim> claims, JwtOptions configuration);

    /// <summary>
    /// 生成 JWT Token（异步，推荐）
    /// </summary>
    /// <param name="claims">用户声明</param>
    /// <param name="configuration">JWT 配置</param>
    /// <returns>Token 生成结果（含 AccessToken / RefreshToken / 过期时间）</returns>
    Task<TokenResult> BuildTokenAsync(IEnumerable<Claim> claims, JwtOptions configuration);

    /// <summary>
    /// 验证 Token（需传入密钥）
    /// </summary>
    Task<TokenValidationResult> JwtSecurityTokenHandlerAsync(
        [Required(ErrorMessage = "privateKey is null!")]
        string privateKey,
        string authorizationString);

    /// <summary>
    /// 验证 Token（使用配置中的密钥）
    /// </summary>
    Task<TokenValidationResult> JwtSecurityTokenHandlerAsync(string authorizationString);

    /// <summary>
    /// 验证 Token 并返回 ClaimsPrincipal
    /// </summary>
    ClaimsPrincipal? ValidateToken(string token, JwtOptions configuration);

    /// <summary>
    /// 刷新 Token
    /// </summary>
    Task<TokenResult> RefreshTokenAsync(string refreshToken, JwtOptions configuration);

    /// <summary>
    /// 吊销 Token（加入黑名单）
    /// </summary>
    Task RevokeTokenAsync(string token, DateTimeOffset? expiresAt = null);
}
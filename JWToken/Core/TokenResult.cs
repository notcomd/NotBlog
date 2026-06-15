namespace Notcomd.Token.JWT.Core;

/// <summary>
/// JWT Token 生成结果
/// </summary>
public class TokenResult
{
    /// <summary>访问 Token（用于 API 认证）</summary>
    public string AccessToken { get; init; } = null!;

    /// <summary>刷新 Token（用于获取新的 AccessToken）</summary>
    public string? RefreshToken { get; init; }

    /// <summary>Token 类型（默认 "Bearer"）</summary>
    public string TokenType { get; init; } = "Bearer";

    /// <summary>过期时间</summary>
    public DateTimeOffset ExpiresAt { get; init; }

    /// <summary>Token 中包含的声明</summary>
    public IReadOnlyList<ClaimInfo> Claims { get; init; } = Array.Empty<ClaimInfo>();
}

/// <summary>
/// 简化的 Claim 信息
/// </summary>
public record ClaimInfo(string Type, string Value);
namespace Identity.Domain.Dto.Request;

/// <summary>
/// 刷新 Token 请求（S-12）：客户端携带 RefreshToken 换取新的 AccessToken/RefreshToken 对。
/// </summary>
public record RefreshTokenRequest(string RefreshToken);

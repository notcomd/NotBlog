namespace Identity.Domain.Dto.Response;

/// <summary>
/// 登录响应
/// </summary>
public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    string TokenType,
    int ExpiresIn,
    string Scope);
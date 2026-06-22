namespace Identity.Web.API.Response;

/// <summary>
/// 登录响应
/// </summary>
public record LoginResponse(
    /// <summary>
    /// 访问令牌
    /// </summary>
    string AccessToken,
    /// <summary>
    /// 刷新令牌
    /// </summary>
    string RefreshToken,
    /// <summary>
    /// 令牌类型
    /// </summary>
    string TokenType,
    /// <summary>
    /// 过期时间（秒）
    /// </summary>
    int ExpiresIn,
    /// <summary>
    /// 作用域
    /// </summary>
    string Scope);
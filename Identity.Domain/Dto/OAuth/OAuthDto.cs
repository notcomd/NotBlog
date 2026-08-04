namespace Identity.Domain.Dto.OAuth;

/// <summary>
///  回调请求
/// </summary>
/// <param name="Code"></param>
/// <param name="State"></param>
/// <param name="RedirectUri"></param>
public record OAuthCallbackRequest(
    string Provider,
    string Code,
    string State,
    string RedirectUri
);

/// <summary>
///  登录初始化请求
/// </summary>
/// <param name="Provider"></param>
/// <param name="RedirectUri"></param>
public record OAuthLoginInitRequest(
    string Provider,
    string RedirectUri
);

/// <summary>
///  登录响应
/// </summary>
/// <param name="AccessToken">访问令牌</param>
/// <param name="RefreshToken">刷新令牌（用于续期 AccessToken）</param>
/// <param name="ExpiresAt">AccessToken 过期时间</param>
/// <param name="UserInfo">用户信息</param>
/// <param name="IsNewUser">是否为本次新建的用户（供 API 层发布注册集成事件）</param>
public record OAuthLoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    UserInfo UserInfo,
    bool IsNewUser = false
);

/// <summary>
///  用户信息
/// </summary>
/// <param name="UserId"></param>
/// <param name="Email"></param>
/// <param name="UserName"></param>
/// <param name="AvatarUrl"></param>
/// <param name="Roles"></param>
public record UserInfo(
    Guid UserId,
    string Email,
    string? UserName,
    Uri? AvatarUrl,
    Guid[] Roles
);

/// <summary>
///  登录提供者信息
/// </summary>
/// <param name="Name"></param>
/// <param name="DisplayName"></param>
/// <param name="Enabled"></param>
public record OAuthProviderInfo(
    string Name,
    string DisplayName,
    bool Enabled
);

/// <summary>
///  外部用户信息
/// </summary>
public class ExternalUserInfo
{
    public string ProviderUserId { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? UserName { get; set; }
    public Uri? AvatarUrl { get; set; }
    
    public Dictionary<string, string?> AdditionalClaims { get; set; } = new();
}
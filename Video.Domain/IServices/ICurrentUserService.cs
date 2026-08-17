namespace Video.Domain.IServices;

/// <summary>
/// 当前登录用户服务 — 从 HttpContext 解析认证后的用户标识。
/// 所有写操作（上传/更新/删除/点赞/收藏）必须使用服务端解析的用户 id，
/// 禁止信任客户端传入的 UserGuid / AffiliatedAuthorizes。
/// </summary>
public interface ICurrentUserService
{
    /// <summary>当前登录用户 Guid；未认证时为 Guid.Empty。</summary>
    bool IsAuthenticated { get; }


    Guid GetUserId();

    string? GetUserRole();
    
    string? GetClaim(string claimType);

    /// <summary>
    /// 判断当前用户是否具有 Admin 角色
    /// </summary>
    bool IsAdmin();

    /// <summary>
    /// 由网关 Middleware 设置当前用户上下文
    /// </summary>
    void SetUser(Guid userId, string[] roles);

    /// <summary>当前请求/连接的原始 Bearer token（供转发到 FileDev gRPC 认证，S-08 客户端侧）</summary>
    string? AccessToken { get; }

    /// <summary>设置当前请求/连接的原始 Bearer token（由 UserContextMiddleware / MessageHub 调用）</summary>
    void SetAccessToken(string? accessToken);
}

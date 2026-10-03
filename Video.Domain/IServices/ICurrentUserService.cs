namespace Video.Domain.IServices;

/// <summary>
/// 当前登录用户服务 — 从 HttpContext 解析认证后的用户标识。
/// 所有写操作（上传/更新/删除/点赞/收藏）必须使用服务端解析的用户 id，
/// 禁止信任客户端传入的 UserGuid / AffiliatedAuthorizes。
/// </summary>
public interface ICurrentUserService
{
    /// <summary>当前请求是否已认证（存在有效用户标识）。</summary>
    bool IsAuthenticated { get; }


    /// <summary>获取当前登录用户 Guid；未认证时返回 Guid.Empty。</summary>
    Guid GetUserId();

    /// <summary>获取当前用户的角色名（逗号分隔字符串）；未认证时返回 null。</summary>
    string? GetUserRole();

    /// <summary>读取当前用户指定 Claim 的值；不存在时返回 null。</summary>
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

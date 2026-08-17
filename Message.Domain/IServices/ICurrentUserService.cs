namespace Message.Domain.IServices;


public interface ICurrentUserService
{
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

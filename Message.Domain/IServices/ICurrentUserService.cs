namespace Message.Domain.IServices;

/// <summary>
/// 当前用户上下文服务接口（提供认证状态与用户标识，由网关中间件填充）。
/// </summary>
public interface ICurrentUserService
{
    /// <summary>当前请求是否已认证</summary>
    bool IsAuthenticated { get; }


    /// <summary>获取当前用户 ID</summary>
    Guid GetUserId();
    /// <summary>获取当前用户角色（不存在返回 null）</summary>
    string? GetUserRole();
    /// <summary>按声明类型获取当前用户的声明值（不存在返回 null）</summary>
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

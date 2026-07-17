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
}

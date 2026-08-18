namespace Markdown.Domain.IServices;

/// <summary>
/// 当前用户服务接口 - 从 JWT Claims 中提取已认证用户信息
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// 获取当前已认证用户的 GUID
    /// </summary>
    Guid GetUserId();

    /// <summary>
    /// 获取当前用户的角色
    /// </summary>
    string? GetUserRole();

    /// <summary>
    /// 判断当前用户是否为管理员角色
    /// 通过检查用户的角色 Claim 是否包含 "Admin" 或 "Root" 来确定用户是否具有管理员权限。
    /// 适用于需要管理员权限的操作或资源访问
    /// </summary>
    /// <returns></returns>
    bool IsAdmin();
    
    /// <summary>
    /// 获取指定的 Claim 值
    /// </summary>
    string? GetClaim(string claimType);
}

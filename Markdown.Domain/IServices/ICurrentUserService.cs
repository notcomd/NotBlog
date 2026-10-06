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
    /// 判断当前用户是否为管理员角色。
    /// 统一口径：兼容 Root / Administrator / Admin（大小写不敏感，支持逗号分隔的多角色 claim），
    /// 适用于需要管理员权限的操作或资源访问。判定依据为角色名 RoleName 而非 RoleCode。
    /// </summary>
    /// <returns></returns>
    bool IsAdmin();
    
    /// <summary>
    /// 获取指定的 Claim 值
    /// </summary>
    string? GetClaim(string claimType);
}

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
    /// 获取指定的 Claim 值
    /// </summary>
    string? GetClaim(string claimType);
}

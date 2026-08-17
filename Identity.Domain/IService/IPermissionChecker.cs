

namespace Identity.Domain.IService;

/// <summary>
/// 权限检查领域服务接口 — 核心权限判定逻辑
/// 
/// 判定链路:
///   userId → User.UserRoleGuid[] → Roles (直连 Permissions)
///                                → RoleGroups (组级 Permissions)
///                                → 合并去重 → 判断是否包含目标 permission_code
/// </summary>
public interface IPermissionChecker
{
    /// <summary>
    /// 检查用户是否拥有指定权限编码
    /// </summary>
    /// <param name="userId">用户 GUID</param>
    /// <param name="permissionCode">权限编码（如 "api:article:read"）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>拥有该权限返回 true，否则 false</returns>
    Task<bool> CheckPermissionAsync(Guid userId, string permissionCode, CancellationToken ct = default);

    /// <summary>
    /// 获取用户所有的权限编码集合（直连 + 组继承，去重）
    /// </summary>
    /// <param name="userId">用户 GUID</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>权限编码集合</returns>
    Task<IReadOnlySet<string>> GetUserPermissionsAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// 获取用户的数据访问范围
    /// </summary>
    /// <param name="userId">用户 GUID</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>数据范围值对象</returns>
    Task<DataScope> GetUserDataScopeAsync(Guid userId, CancellationToken ct = default);
}

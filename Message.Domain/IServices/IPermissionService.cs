﻿namespace Message.Domain.IServices;

/// <summary>
/// 权限验证服务接口
/// 基于角色权限模型 (RolePermission) 验证用户是否有权访问 API
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// 检查当前用户是否拥有指定权限编码
    /// </summary>
    /// <param name="permissionCode">权限编码（如 "message:send"、"user:create"）</param>
    Task<bool> HasPermissionAsync(string permissionCode);

    /// <summary>
    /// 检查指定用户是否拥有指定权限编码
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="permissionCode">权限编码</param>
    Task<bool> HasPermissionAsync(Guid userId, string permissionCode);

    /// <summary>
    /// 获取用户所有权限编码列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    Task<IEnumerable<string>> GetUserPermissionsAsync(Guid userId);

    /// <summary>
    /// 检查当前用户是否拥有指定角色
    /// </summary>
    /// <param name="roleName">角色名称或编码</param>
    Task<bool> HasRoleAsync(string roleName);

    /// <summary>
    /// 获取用户所有角色
    /// </summary>
    Task<IEnumerable<string>> GetUserRolesAsync(Guid userId);
}
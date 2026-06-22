// 注：此接口依赖的 RolePermission 类型已在 RBAC 重构中移除。
// 如需恢复权限功能，请重新定义 RolePermission 实体后再取消注释。
//
// using System.Collections.Generic;
// using System.Threading.Tasks;
//
// namespace Identity.Domain.IService;
//
// public interface IPermissionService
// {
//     Task CreateAsync(RolePermission rolePermission);
//     Task DeleteAsync(Guid permissionId);
//     Task UpdateAsync(RolePermission rolePermission);
//     Task<RolePermission> GetAsync(Guid permissionId);
//     Task<IReadOnlyCollection<RolePermission>> GetAllAsync();
//     Task<bool> ExistsAsync(Guid permissionId);
//     Task<bool> ExistsAsync(string permissionName);
//     Task<RolePermission> GetByNameAsync(string permissionName);
//     Task<List<RolePermission>> GetAllByNameAsync(List<string> permissionNames);
// }


namespace Identity.Domain.IRepository;

public interface IRoleGroupRepository : IRepository<RoleGroup, IUnitOfWork>
{
    /// <summary>
    /// 根据角色ID获取角色组
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <returns>角色组</returns>
    ValueTask<RoleGroup?> FindOneByRoleAsync(Guid roleId);

    /// <summary>
    /// 根据角色ID获取所有角色组
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <returns>角色组列表</returns>
    Task<ICollection<RoleGroup>> FindAllByRoleAsync(Guid roleId);


    /// <summary>
    /// 添加角色组
    /// </summary>
    /// <param name="roleGroup">角色组</param>
    ValueTask AddOneByRoleGroupAsync(RoleGroup roleGroup);

    /// <summary>
    /// 删除角色组
    /// </summary>
    /// <param name="roleGroupId">角色组ID</param>
    ValueTask DeleteOneByRoleGroupAsync(Guid roleGroupId);


    /// <summary>
    /// 更新角色组
    /// </summary>
    /// <param name="roleGroup">角色组</param>
    ValueTask UpdateOneByRoleGroupAsync(RoleGroup roleGroup);


    /// <summary>
    /// 获取所有角色组
    /// </summary>
    /// <returns>角色组列表</returns>
    Task<ICollection<RoleGroup>> GetAllAsync();

    /// <summary>
    /// 根据角色组 ID 加载角色组并包含权限导航（角色组权限管理用；修改 Permissions 集合前必须加载导航）
    /// </summary>
    /// <param name="roleGroupGuid">角色组 ID</param>
    /// <returns>角色组</returns>
    ValueTask<RoleGroup?> FindByRoleGroupWithPermissionsAsync(Guid roleGroupGuid);

    /// <summary>
    /// 获取全部角色组并包含权限导航（角色组列表 PermissionCount 统计用）
    /// </summary>
    /// <returns>角色组列表</returns>
    Task<ICollection<RoleGroup>> GetAllWithPermissionsAsync();
}
namespace Identity.Domain.IRepository;

public interface IUserRoleRepository : IRepository<Roles, IUnitOfWork>
{
    ValueTask AddByUserRoleAsync(Roles userRole);

    ValueTask<Roles?> FindByUserRoleAsync(Guid roleGuid);

    /// <summary>
    /// 加载角色并包含直连权限导航（角色权限管理用；修改 Permissions 集合前必须加载导航）
    /// </summary>
    ValueTask<Roles?> FindByUserRoleWithPermissionsAsync(Guid roleGuid);

    ValueTask<HashSet<Roles>?> FindByUserRoleAsync(HashSet<Guid> roleGuid);

    ValueTask<Roles?> FindUserIdByRoleAsync(Guid userId);

    ValueTask<Roles?> FindByUserRoleAsync(string roleName);

    ValueTask<bool> IsUserRoleAsync(Guid guid);

    ValueTask<bool> IsUserRoleAsync(string roleName);

    ValueTask<bool> UpByUserRoleAsync(Roles userRole);
}
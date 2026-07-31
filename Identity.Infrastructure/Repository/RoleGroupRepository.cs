namespace Identity.Infrastructure.Repository;

public class RoleGroupRepository(IdentityDbContext dbContext) : IRoleGroupRepository
{
    private readonly IdentityDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public IUnitOfWork UnitOfWork => _dbContext;


    /// <summary>
    /// 根据角色 ID 获取包含该角色的角色组
    /// </summary>
    /// <param name="roleId">角色 ID</param>
    /// <returns>角色组</returns>
    public async ValueTask<RoleGroup?> FindOneByRoleAsync(Guid roleId)
    {
        return await _dbContext.RoleGroups
            .Where<RoleGroup>(x => x.RoleGuids.Contains(roleId))
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// 根据角色 ID 获取所有包含该角色的角色组
    /// </summary>
    /// <param name="roleId">角色 ID</param>
    /// <returns>角色组列表</returns>
    public async Task<ICollection<RoleGroup>> FindAllByRoleAsync(Guid roleId)
    {
        return await _dbContext.RoleGroups
            .Where<RoleGroup>(x => x.RoleGuids.Contains(roleId))
            .ToListAsync();
    }


    /// <summary>
    /// 添加角色组
    /// </summary>
    /// <param name="roleGroup">角色组</param>
    public async ValueTask AddOneByRoleGroupAsync(RoleGroup roleGroup)
    {
        await _dbContext.RoleGroups.AddAsync(roleGroup);
    }

    /// <summary>
    /// 删除角色组
    /// </summary>
    /// <param name="roleGroupId">角色组 ID</param>
    public async ValueTask DeleteOneByRoleGroupAsync(Guid roleGroupId)
    {
        var entity = await _dbContext.RoleGroups.FindAsync(roleGroupId);
        if (entity is not null)
            _dbContext.RoleGroups.Remove(entity);
    }


    /// <summary>
    /// 更新角色组
    /// </summary>
    /// <param name="roleGroup">角色组</param>
    public async ValueTask UpdateOneByRoleGroupAsync(RoleGroup roleGroup)
    {
        _dbContext.RoleGroups.Update(roleGroup);
    }


    /// <summary>
    /// 获取所有角色组
    /// </summary>
    /// <returns>角色组列表</returns>
    public async Task<ICollection<RoleGroup>> GetAllAsync()
    {
        return await _dbContext.RoleGroups.ToListAsync();
    }
}

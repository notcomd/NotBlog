namespace Identity.Infrastructure.Repository;

public class RoleGroupRepository(IdentityDbContext dbContext) : IRoleGroupRepository
{
    private readonly IdentityDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public IUnitOfWork UnitOfWork => _dbContext;


    /// <summary>
    /// 根据角色ID获取角色组
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <returns>角色组</returns>
    public async ValueTask<RoleGroup?> FindOneByRoleAsync(Guid roleId)
    {
        return await _dbContext.RoleGroups
            .Include(x => x.Roles)
            .Where<RoleGroup>(x => x.Roles.Any(en => en.RoleGuid == roleId))
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// 根据角色ID获取所有角色组
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <returns>角色组列表</returns>
    public async Task<ICollection<RoleGroup>> FindAllByRoleAsync(Guid roleId)
    {
        return await _dbContext.RoleGroups
            .Include(x => x.Roles)
            .Where<RoleGroup>(x => x.Roles.Any(en => en.RoleGuid == roleId))
            .ToListAsync();
    }


    /// <summary>
    /// 添加角色组
    /// </summary>
    /// <param name="roleGroup">角色组</param>
    /// <param name="roleGroup">角色组</param>
    public async ValueTask AddOneByRoleGroupAsync(RoleGroup roleGroup)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 删除角色组
    /// </summary>
    /// <param name="roleGroupId">角色组ID</param>
    public async ValueTask DeleteOneByRoleGroupAsync(Guid roleGroupId)
    {
        _dbContext.RoleGroups.Attach(await _dbContext.RoleGroups.FindAsync(roleGroupId));
        _dbContext.RoleGroups.Remove(await _dbContext.RoleGroups.FindAsync(roleGroupId));
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
namespace Identity.Infrastructure.Repository;

public class UserRoleRepository(IdentityDbContext userRoleDbContext) : IUserRoleRepository
{
    public IUnitOfWork UnitOfWork => userRoleDbContext;


    public async ValueTask AddByUserRoleAsync(Roles userRole)
    {
        await userRoleDbContext.AddAsync(userRole);
    }

    public async ValueTask<Roles?> FindByUserRoleAsync(Guid guid)
    {
        return await userRoleDbContext.FindAsync<Roles>(guid);
    }

    public async ValueTask<Roles?> FindByUserRoleWithPermissionsAsync(Guid roleGuid)
    {
        // ⚠️ 用 FirstOrDefault：Include(Permissions) 集合导航会把父行 join 展开成多行，
        // SingleOrDefaultAsync 在子权限多于 1 条时抛 "Sequence contains more than one element"
        return await userRoleDbContext.Roles
            .Include(r => r.Permissions)
            .Where(r => r.RoleGuid == roleGuid)
            .FirstOrDefaultAsync();
    }

    public async Task<List<Roles>> FindAllWithPermissionsAsync()
    {
        return await userRoleDbContext.Roles
            .Include(r => r.Permissions)
            .ToListAsync();
    }

    public async ValueTask<HashSet<Roles>?> FindByUserRoleAsync(HashSet<Guid>? roleGuid)
    {
        if (roleGuid is null || roleGuid.Count == 0)
            return new HashSet<Roles>();
        var roles = await userRoleDbContext.Roles
            .Where(en => roleGuid.Contains(en.RoleGuid))
            .AsNoTracking().ToHashSetAsync();
        return roles;
    }

    public async ValueTask<Roles?> FindUserIdByRoleAsync(Guid userId)
    {
        if (Guid.Empty == userId)
            return null;

        var roleData = await userRoleDbContext
            .Roles.Where(en => en.RoleGuid.Equals(userId))
            .FirstOrDefaultAsync();
        return roleData;
    }

    public async ValueTask<Roles?> FindByUserRoleAsync(string roleName)
    {
        return await userRoleDbContext.Roles
            .Include(en => en.Permissions)
            .Where(en => en.RoleName == roleName)
            .SingleOrDefaultAsync();
    }

    public async ValueTask<bool> IsUserRoleAsync(Guid guid)
    {
        if (await FindByUserRoleAsync(guid) is null) return false;
        return true;
    }

    public ValueTask<bool> IsUserRoleAsync(string roleName)
    {
        return ValueTask.FromResult(userRoleDbContext.Roles.Any(en => en.RoleName.Equals(roleName)));
    }

    public async ValueTask<bool> UpByUserRoleAsync(Roles userRole)
    {
        var existing = await FindByUserRoleAsync(userRole.RoleGuid);
        if (existing is null)
            return false;
        userRoleDbContext.Update(userRole);
        return true;
    }
}
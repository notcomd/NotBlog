namespace Identity.Infrastructure.Repository;

public class UserRoleRepository : IUserRoleRepository
{
    private readonly IdentityDbContext _userRoleDbContext;

    public UserRoleRepository(IdentityDbContext userRoleDbContext)
    {
        _userRoleDbContext = userRoleDbContext;
    }

    public IUnitOfWork UnitOfWork => _userRoleDbContext;


    public async ValueTask AddByUserRoleAsync(Roles userRole)
    {
        await _userRoleDbContext.AddAsync(userRole);
    }

    public async ValueTask<Roles?> FindByUserRoleAsync(Guid guid)
    {
        var data = await _userRoleDbContext.FindAsync<Roles>(guid);
        if (data is null) throw new ArgumentNullException("data is null");
        return data;
    }

    public async ValueTask<HashSet<Roles>?> FindByUserRoleAsync(HashSet<Guid> roleGuid)
    {
        if (roleGuid is null || roleGuid.Count == 0)
            return new HashSet<Roles>();
        var roles = await _userRoleDbContext.Roles
            .Where(en => roleGuid.Contains(en.RoleGuid))
            .AsNoTracking().ToHashSetAsync();
        return roles;
    }

    public async ValueTask<Roles?> FindUserIdByRoleAsync(Guid userId)
    {
        if (Guid.Empty == userId)
            return null;

        var roleData = await _userRoleDbContext
            .Roles.Where(en => en.RoleGuid.Equals(userId))
            .FirstOrDefaultAsync();
        return roleData;
    }

    public async ValueTask<Roles?> FindByUserRoleAsync(string roleName)
    {
        var data = await
            _userRoleDbContext.Roles
                .Include(en => en.RolePermission)
                .Where(en => en.RoleName == roleName)
                .SingleOrDefaultAsync();
        if (data is null) throw new ArgumentNullException("data is null!");
        return data;
    }

    public async ValueTask<bool> IsUserRoleAsync(Guid guid)
    {
        if (await FindByUserRoleAsync(guid) is null) return false;
        return true;
    }

    public async ValueTask<bool> IsUserRoleAsync(string roleName)
    {
        return _userRoleDbContext.Roles.Any(en => en.RoleName == roleName);
    }

    public async ValueTask<bool> UpByUserRoleAsync(Roles userRole)
    {
        if (await FindByUserRoleAsync(userRole.RoleGuid) == userRole) return true;
        _userRoleDbContext.Update(userRole);
        return true;
    }
}
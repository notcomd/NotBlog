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

    public async ValueTask<Roles?> FindByUserRoleAsync(string roleName)
    {
        var data = await _userRoleDbContext.FindAsync<Roles>(roleName);
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
        if (await FindByUserRoleAsync(roleName) is null) return false;
        return true;
    }

    public async ValueTask<bool> UpByUserRoleAsync(Roles userRole)
    {
        if (await FindByUserRoleAsync(userRole.RoleGuid) == userRole) return true;
        _userRoleDbContext.Update(userRole);
        return true;
    }
}
using Identity.Domain.Entities;
using Identity.Domain.IRepository;
using Identity.Infrastructure.EntityFramework;

namespace Identity.Infrastructure.Repository;

public class UserRoleRepository : IUserRoleRepository
{
    private readonly UserDbContext _userRoleDbContext;

    public UserRoleRepository(UserDbContext userRoleDbContext)
    {
        _userRoleDbContext = userRoleDbContext;
    }


    public async ValueTask AddByUserRoleAsync(UserRole userRole)
    {
        await _userRoleDbContext.AddAsync(userRole);
    }

    public async ValueTask<UserRole?> FindByUserRoleAsync(Guid guid)
    {
        var data = await _userRoleDbContext.FindAsync<UserRole>(guid);
        if (data is null) throw new ArgumentNullException("data is null");
        return data;
    }

    public async ValueTask<UserRole?> FindByUserRoleAsync(string roleName)
    {
        var data = await _userRoleDbContext.FindAsync<UserRole>(roleName);
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

    public async ValueTask<bool> UpByUserRoleAsync(UserRole userRole)
    {
        if (await FindByUserRoleAsync(userRole.UserRoleGuid) == userRole) return true;
        _userRoleDbContext.Update(userRole);
        return true;
    }
}
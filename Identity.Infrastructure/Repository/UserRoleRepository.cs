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
        var data = await userRoleDbContext.FindAsync<Roles>(guid);
        return data ?? throw new ArgumentNullException(nameof(data));
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
        var data = await
            userRoleDbContext.Roles
                .Include(en => en.Permissions)
                .Where(en => en.RoleName == roleName)
                .SingleOrDefaultAsync();
        return data ?? throw new ArgumentNullException($"{roleName}");
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
        if ((await FindByUserRoleAsync(userRole.RoleGuid) ?? throw new InvalidOperationException()) == userRole) return true;
        userRoleDbContext.Update(userRole);
        return true;
    }
}
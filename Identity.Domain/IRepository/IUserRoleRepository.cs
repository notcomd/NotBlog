namespace Identity.Domain.IRepository;

public interface IUserRoleRepository : IRepository<Roles>
{
    ValueTask AddByUserRoleAsync(Roles userRole);

    ValueTask<Roles?> FindByUserRoleAsync(Guid roleGuid);
    
    ValueTask<HashSet<Roles>?> FindByUserRoleAsync(HashSet<Guid> roleGuid);
    
    ValueTask<Roles?> FindUserIdByRoleAsync(Guid userId);

    ValueTask<Roles?> FindByUserRoleAsync(string roleName);

    ValueTask<bool> IsUserRoleAsync(Guid guid);

    ValueTask<bool> IsUserRoleAsync(string roleName);

    ValueTask<bool> UpByUserRoleAsync(Roles userRole);
}
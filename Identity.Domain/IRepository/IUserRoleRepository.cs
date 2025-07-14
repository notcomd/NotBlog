namespace Identity.Domain.IRepository;

public interface IUserRoleRepository : IRepository<Roles>
{
    ValueTask AddByUserRoleAsync(Roles userRole);

    ValueTask<Roles?> FindByUserRoleAsync(Guid guid);

    ValueTask<Roles?> FindByUserRoleAsync(string roleName);

    ValueTask<bool> IsUserRoleAsync(Guid guid);

    ValueTask<bool> IsUserRoleAsync(string roleName);

    ValueTask<bool> UpByUserRoleAsync(Roles userRole);
}
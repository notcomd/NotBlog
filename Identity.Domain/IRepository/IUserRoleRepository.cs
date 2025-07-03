namespace Identity.Domain.IRepository;

public interface IUserRoleRepository
{
    ValueTask AddByUserRoleAsync(UserRole userRole);

    ValueTask<UserRole?> FindByUserRoleAsync(Guid guid);

    ValueTask<UserRole?> FindByUserRoleAsync(string roleName);

    ValueTask<bool> IsUserRoleAsync(Guid guid);

    ValueTask<bool> IsUserRoleAsync(string roleName);

    ValueTask<bool> UpByUserRoleAsync(UserRole userRole);
}
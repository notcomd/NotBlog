namespace Identity.Infrastructure.Services;

public class UserRoleService : IUserRoleService
{
    public async Task<Roles> FindByUserIdAndRoleIdAsync(Guid userId, Guid roleId)
    {
        throw new NotImplementedException();
    }

    public async Task<ICollection<Roles>> FindByUserIdAsync(Guid userId)
    {
        throw new NotImplementedException();
    }

    public async Task<ICollection<Roles>> FindByRoleIdAsync(Guid roleId)
    {
        throw new NotImplementedException();
    }

    public async Task<Roles> FindByRoleNameAsync(string roleName)
    {
        throw new NotImplementedException();
    }
}
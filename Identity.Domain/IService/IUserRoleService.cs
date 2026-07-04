namespace Identity.Domain.IService;

public interface IUserRoleService
{
    Task<Roles> FindByUserIdAndRoleIdAsync(Guid userId, Guid roleId);

    Task<ICollection<Roles>> FindByUserIdAsync(Guid userId);

    Task<ICollection<Roles>> FindByRoleIdAsync(Guid roleId);

    Task<Roles> FindByRoleNameAsync(string roleName);
}
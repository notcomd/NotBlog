namespace Identity.Domain.IRepository;

public interface IUserRoleRepository : IRepository<Roles>
{
    ValueTask AddByUserRoleAsync(Roles userRole);

    ValueTask<IEnumerable<Roles>> FindByUserRolesAsync();

    ValueTask<Roles?> FindByUserRoleAsync(Guid guid);

    ValueTask<Roles?> FindByUserRoleAsync(string roleName);

    ValueTask<bool> IsUserRoleAsync(Guid guid);

    ValueTask<bool> IsUserRoleAsync(string roleName);

    ValueTask UpByUserRoleAsync(Roles userRole);

    ValueTask <IEnumerable<RoleClaim>> FindRoleClaimByRolesAsync(Guid roleGuid);

    ValueTask  UpdateWithRoleAsync(Guid roleGuid,Func<Roles,Task> func);

    ValueTask  UpdateWithRoleAsync(string roleName, Func<Roles,Task> func);

    ValueTask DeleteByUserRoleAsync(Guid roleGuid);

}
namespace Identity.Infrastructure.Services;

/// <summary>
/// 用户角色服务（F-07：当前无业务调用方，显式降级返回空结果，替代原 NotImplementedException）
/// </summary>
public class UserRoleService : IUserRoleService
{
    // TODO(F-07): 需要真实查询时，注入 IUserRepository 读取 User.UserRoleGuid 后批量查询角色

    public Task<Roles> FindByUserIdAndRoleIdAsync(Guid userId, Guid roleId)
    {
        return Task.FromResult<Roles>(null!);
    }

    public Task<ICollection<Roles>> FindByUserIdAsync(Guid userId)
    {
        return Task.FromResult<ICollection<Roles>>(Array.Empty<Roles>());
    }

    public Task<ICollection<Roles>> FindByRoleIdAsync(Guid roleId)
    {
        return Task.FromResult<ICollection<Roles>>(Array.Empty<Roles>());
    }

    public Task<Roles> FindByRoleNameAsync(string roleName)
    {
        return Task.FromResult<Roles>(null!);
    }
}
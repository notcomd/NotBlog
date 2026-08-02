namespace Identity.Infrastructure.Services;

/// <summary>
/// 角色组服务（F-07：补齐真实实现，替代原 NotImplementedException 占位）
/// </summary>
public class RoleGroupService(IRoleGroupRepository roleGroupRepository) : IRoleGroupService
{
    public async Task CreateAsync(RoleGroup roleGroup)
    {
        if (roleGroup is null)
            throw new ArgumentNullException(nameof(roleGroup));

        await roleGroupRepository.AddOneByRoleGroupAsync(roleGroup);
        await roleGroupRepository.UnitOfWork.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid groupId)
    {
        await roleGroupRepository.DeleteOneByRoleGroupAsync(groupId);
        await roleGroupRepository.UnitOfWork.SaveChangesAsync();
    }

    public async Task UpdateAsync(RoleGroup roleGroup)
    {
        if (roleGroup is null)
            throw new ArgumentNullException(nameof(roleGroup));

        await roleGroupRepository.UpdateOneByRoleGroupAsync(roleGroup);
        await roleGroupRepository.UnitOfWork.SaveChangesAsync();
    }

    public async Task<RoleGroup> GetAsync(Guid groupId)
    {
        var all = await roleGroupRepository.GetAllAsync();
        return all.FirstOrDefault(g => g.RoleGroupGuid == groupId)
               ?? throw new KeyNotFoundException($"角色组不存在: {groupId}");
    }

    public async Task<IReadOnlyCollection<RoleGroup>> GetAllAsync()
    {
        return (await roleGroupRepository.GetAllAsync()).ToList();
    }

    public async Task<IReadOnlyCollection<RoleGroup>> GetAllByNameAsync(List<string> groupNames)
    {
        if (groupNames is null || groupNames.Count == 0)
            return Array.Empty<RoleGroup>();

        var all = await roleGroupRepository.GetAllAsync();
        return all
            .Where(g => groupNames.Contains(g.RoleGroupName, StringComparer.OrdinalIgnoreCase))
            .ToList();
    }
}
namespace Identity.Domain.IService;

public interface IRoleGroupService
{
    Task CreateAsync(RoleGroup roleGroup);

    Task DeleteAsync(Guid groupId);

    Task UpdateAsync(RoleGroup roleGroup);

    Task<RoleGroup> GetAsync(Guid groupId);

    Task<IReadOnlyCollection<RoleGroup>> GetAllAsync();

    Task<IReadOnlyCollection<RoleGroup>> GetAllByNameAsync(List<string> groupNames);
}
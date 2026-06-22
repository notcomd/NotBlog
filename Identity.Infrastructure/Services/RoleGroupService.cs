namespace Identity.Infrastructure.Services;

public class RoleGroupService : IRoleGroupService
{
    private readonly IRoleGroupRepository _roleGroupRepository;

    public RoleGroupService(IRoleGroupRepository roleGroupRepository)
    {
        _roleGroupRepository = roleGroupRepository;
    }

    public async Task CreateAsync(RoleGroup roleGroup)
    {
        throw new NotImplementedException();
    }

    public async Task DeleteAsync(Guid groupId)
    {
        throw new NotImplementedException();
    }

    public async Task UpdateAsync(RoleGroup roleGroup)
    {
        throw new NotImplementedException();
    }

    public async Task<RoleGroup> GetAsync(Guid groupId)
    {
        throw new NotImplementedException();
    }

    public async Task<IReadOnlyCollection<RoleGroup>> GetAllAsync()
    {
        throw new NotImplementedException();
    }

    public async Task<IReadOnlyCollection<RoleGroup>> GetAllByNameAsync(List<string> groupNames)
    {
        throw new NotImplementedException();
    }
}
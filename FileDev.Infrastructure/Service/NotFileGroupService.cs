
using FileDev.Domain.IRepository;
using Microsoft.Extensions.Logging;
namespace FileDev.Infrastructure.Service;

public class NotFileGroupService(INotFileGroupRepository notFileGroupRepository, ILogger<NotFileGroupService> logger)
    : INotFileGroupService
{
    public async Task<IEnumerable<NotFileGroup>> GetNotFileGroupsByUserIdAsync(Guid userId)
    {
        var listData = await notFileGroupRepository.GetNotFileGroupsByUserIdAsync(userId);
        return listData.Where(en => !en.IsDeleted);
    }

    public async Task<NotFileGroup> GetNotFileGroupByIdAsync(Guid notFileGroupId)
    {
        return await notFileGroupRepository.GetNotFileGroupByIdAsync(notFileGroupId);
    }

    public async Task<IEnumerable<NotFileGroup>> GetRootGroupsByUserIdAsync(Guid userId)
    {
        return await notFileGroupRepository.GetRootGroupsByUserIdAsync(userId);
    }

    public async Task<IEnumerable<NotFileGroup>> GetChildrenAsync(Guid parentGroupId)
    {
        return await notFileGroupRepository.GetChildrenAsync(parentGroupId);
    }
}
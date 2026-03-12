using FileDev.Domain.Entities;
using FileDev.Domain.IRepository;

namespace FileDev.Domain.Services;

public class NotFileGroupService(INotFileGroupRepository notFileGroupRepository)
{
    
    private readonly INotFileGroupRepository _notFileGroupRepository=notFileGroupRepository
                                                                     ??throw new ArgumentNullException(nameof(notFileGroupRepository));
    
    
    public async Task CreateNotFileGroupAsync(Guid userId, string groupName, FileType fileType)
    {
        var notifier = new NotFileGroup.NotFileGroupBuilder()
            .WithUserId(userId)
            .WithFileGroupName(groupName)
            .WithFileType(fileType)
            .Build();
        await _notFileGroupRepository.InsetNotFileGroupAsync(notifier);
    }
    
    
    public async Task<IEnumerable<NotFileGroup>> GetNotFileGroupsByUserIdAsync(Guid userId)
    {
        return await _notFileGroupRepository.GetNotFileGroupsByUserIdAsync(userId);
    }
    
}
using FileDev.Domain.Entities;
using FileDev.Domain.SeedWork;
namespace FileDev.Domain.IRepository;

public interface INotFileGroupRepository: IRepository<NotFileGroup>
{
    
    Task InsertNotFileGroupAsync(NotFileGroup notFileGroup);
    
    Task<NotFileGroup> GetNotFileGroupByIdAsync(Guid notFileGroupId);
    
    Task<IEnumerable<NotFileGroup>> GetAllNotFileGroupsAsync();
    
    Task<IEnumerable<NotFileGroup>> GetNotFileGroupsByUserIdAsync(Guid userId);
    
    Task<IEnumerable<NotFileGroup>> GetPublicNotFileGroupsAsync();
    
    // Task<IEnumerable<NotFileGroup>> GetNotFileGroupsByTypeAsync(FileType fileType);
    
    Task<NotFileGroup?> GetNotFileGroupByNameAsync(string fileGroupName);
    
    Task<NotFileGroup?> UpdateNotFileGroupAsync(NotFileGroup notFileGroup);
    
    Task DeleteNotFileGroupAsync(Guid notFileGroupId);
    
}
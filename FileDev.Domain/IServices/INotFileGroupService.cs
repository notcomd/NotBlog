using FileDev.Domain.Entities;

namespace FileDev.Domain.IServices;

public interface INotFileGroupService
{
    public Task CreateNotFileGroupAsync(Guid userId, string groupName, string? groupDescription,
        HashSet<string>? tags = null, FileType fileType = FileType.CompressFiles,FileIdentity fileIdentity = FileIdentity.FilePublic);
    
    public Task<IEnumerable<NotFileGroup>> GetNotFileGroupsByUserIdAsync(Guid userId);
    
    public  Task<NotFileGroup> GetNotFileGroupByIdAsync(Guid notFileGroupId);
    
    public Task AddFileToNotFileGroupAsync(Guid notFileGroupId, Guid fileId);
    
    public Task RemoveFileFromNotFileGroupAsync(Guid notFileGroupId, Guid fileId);

    public Task UpdateNotFileGroupAsync(Guid notFileGroupId, string groupName, string? groupDescription,
        HashSet<string>? tags = null);
    
    public Task DeleteNotFileGroupAsync(Guid notFileGroupId);
}
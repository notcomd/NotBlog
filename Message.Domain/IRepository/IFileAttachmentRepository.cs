using Message.Domain.Entities;
using Message.Domain.SeedWork;

namespace Message.Domain.IRepository;
/// <summary>
/// 文件附件仓储接口
/// </summary>
public interface IFileAttachmentRepository : IRepository<FileAttachment>
{
    Task<FileAttachment?> GetByIdAsync(Guid attachmentId);
    Task<IEnumerable<FileAttachment>> GetByMessageIdAsync(Guid messageId);
    Task<IEnumerable<FileAttachment>> GetByFileTypeAsync(string fileType);
    Task<FileAttachment> AddAsync(FileAttachment attachment);
    Task<FileAttachment> UpdateAsync(FileAttachment attachment);
    Task DeleteAsync(Guid attachmentId);
    Task<bool> ExistsAsync(Guid attachmentId);
    Task<int> CountByMessageAsync(Guid messageId);
}
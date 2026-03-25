using Message.Domain.Entities;

namespace Message.Domain.IRepository;

public interface IFileAttachmentRepository
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
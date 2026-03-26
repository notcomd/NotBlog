using Message.Domain.Entities;
using Message.Domain.IRepository;
using Message.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Message.Infrastructure.Repository;

public class FileAttachmentRepository(MessageDbContext context) : IFileAttachmentRepository
{
    private readonly DbSet<FileAttachment> _dbSet = context.Set<FileAttachment>();

    public async Task<FileAttachment?> GetByIdAsync(Guid attachmentId)
    {
        return await _dbSet.FirstOrDefaultAsync(f => f.AttachmentId == attachmentId);
    }

    public async Task<IEnumerable<FileAttachment>> GetByMessageIdAsync(Guid messageId)
    {
        return await _dbSet.Where(f => f.MessageId == messageId && !f.IsDeleted).ToListAsync();
    }

    public async Task<IEnumerable<FileAttachment>> GetByFileTypeAsync(string fileType)
    {
        return await _dbSet.Where(f => f.FileType.StartsWith(fileType) && !f.IsDeleted).ToListAsync();
    }

    public async Task<FileAttachment> AddAsync(FileAttachment attachment)
    {
        var entry = await _dbSet.AddAsync(attachment);
        return entry.Entity;
    }

    public async Task<FileAttachment> UpdateAsync(FileAttachment attachment)
    {
        var entry = _dbSet.Update(attachment);
        return entry.Entity;
    }

    public async Task DeleteAsync(Guid attachmentId)
    {
        var attachment = await GetByIdAsync(attachmentId);
        if (attachment is not null)
        {
            attachment.Delete();
            _dbSet.Update(attachment);
        }
    }

    public async Task<bool> ExistsAsync(Guid attachmentId)
    {
        return await _dbSet.AnyAsync(f => f.AttachmentId == attachmentId && !f.IsDeleted);
    }

    public async Task<int> CountByMessageAsync(Guid messageId)
    {
        return await _dbSet.CountAsync(f => f.MessageId == messageId && !f.IsDeleted);
    }
}
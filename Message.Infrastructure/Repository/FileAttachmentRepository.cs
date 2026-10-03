
namespace Message.Infrastructure.Repository;

/// <summary>文件附件仓储实现，负责 FileAttachments 表的查询与持久化。</summary>
public class FileAttachmentRepository(MessageDbContext context) : IFileAttachmentRepository
{
    private readonly DbSet<FileAttachment> _dbSet = context.Set<FileAttachment>();

    /// <summary>按附件 ID 获取附件，不存在时返回 null。</summary>
    public async Task<FileAttachment?> GetByIdAsync(Guid attachmentId)
    {
        return await _dbSet.FirstOrDefaultAsync(f => f.AttachmentId == attachmentId);
    }

    /// <summary>获取指定消息下未删除的附件集合。</summary>
    public async Task<IEnumerable<FileAttachment>> GetByMessageIdAsync(Guid messageId)
    {
        return await _dbSet.Where(f => f.MessageId == messageId && !f.IsDeleted).ToListAsync();
    }

    /// <summary>按文件类型前缀获取未删除的附件集合。</summary>
    public async Task<IEnumerable<FileAttachment>> GetByFileTypeAsync(string fileType)
    {
        return await _dbSet.Where(f => f.FileType.StartsWith(fileType) && !f.IsDeleted).ToListAsync();
    }

    /// <summary>新增附件并返回已跟踪的实体。</summary>
    public async Task<FileAttachment> AddAsync(FileAttachment attachment)
    {
        var entry = await _dbSet.AddAsync(attachment);
        return entry.Entity;
    }

    /// <summary>更新附件并返回已跟踪的实体。</summary>
    public async Task<FileAttachment> UpdateAsync(FileAttachment attachment)
    {
        var entry = _dbSet.Update(attachment);
        return entry.Entity;
    }

    /// <summary>软删除指定附件。</summary>
    public async Task DeleteAsync(Guid attachmentId)
    {
        var attachment = await GetByIdAsync(attachmentId);
        if (attachment is not null)
        {
            attachment.Delete();
            _dbSet.Update(attachment);
        }
    }

    /// <summary>判断指定未删除的附件是否存在。</summary>
    public async Task<bool> ExistsAsync(Guid attachmentId)
    {
        return await _dbSet.AnyAsync(f => f.AttachmentId == attachmentId && !f.IsDeleted);
    }

    /// <summary>统计指定消息下未删除的附件数量。</summary>
    public async Task<int> CountByMessageAsync(Guid messageId)
    {
        return await _dbSet.CountAsync(f => f.MessageId == messageId && !f.IsDeleted);
    }
}
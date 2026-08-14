using FileDev.Domain.Entities;
using FileDev.Domain.Exception;
using FileDev.Domain.IRepository;
using FileDev.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace FileDev.Infrastructure.Repository;

public class NotFileRepository(NotFileDbContext notFileDbContext) : INotFileRepository
{
    private readonly NotFileDbContext _notFileDbContext =
        notFileDbContext ?? throw new ArgumentNullException(nameof(notFileDbContext));

    public IUnitOfWork UnitOfWork => notFileDbContext;

    /// <summary>
    /// 注意：不加 IsDeleted 过滤 — FileDeletedEventHandler 需要在软删除后仍能查到文件记录以执行物理文件清理。
    /// 软删除过滤由上层 NotFileService.GetFileByIdAsync 处理（IsDeleted == true 时返回 null）。
    /// </summary>
    public async Task<NotFile?> GetFileByIdAsync(Guid fileId)
    {
        return await _notFileDbContext.NotFiles
            .FirstOrDefaultAsync(x => x.FileId.Equals(fileId));
    }

    public async Task<IEnumerable<NotFile>?> GetAllFilesAsync()
    {
        return await _notFileDbContext.NotFiles
            .AsNoTracking()
            .Where(en => !en.IsDeleted).ToListAsync();
    }

    public async Task<IEnumerable<NotFile>> GetFilesByUserIdAsync(Guid userId)
    {
        if (Guid.Empty == userId)
            throw new NotFileException("userId is null");
        // 在 DB 层过滤 IsDeleted，避免加载已删除文件到内存后再过滤（原实现在 Service 层内存过滤，低效）
        return await _notFileDbContext.NotFiles
            .AsNoTracking()
            .Where(x => x.UserId.Equals(userId) && !x.IsDeleted)
            .ToListAsync();
    }

    public async Task<IEnumerable<NotFile>> GetPublicFilesAsync()
    {
        return await _notFileDbContext.NotFiles
            .AsNoTracking()
            .Where(x => x.FileIdentity.Equals(FileIdentity.FilePublic) && !x.IsDeleted)
            .ToListAsync();
    }

    public async Task<IEnumerable<NotFile>?> GetFilesByTagsAsync(HashSet<string> tags)
    {
        return await _notFileDbContext.NotFiles
            .AsNoTracking()
            .Where(x => x.FileTags.Any(t => tags.Contains(t)) && !x.IsDeleted)
            .ToListAsync();
    }

    public async Task InsertFileAsync(NotFile file)
    {
        await notFileDbContext.NotFiles.AddAsync(file);
    }

    public Task<bool> UpdateFileAsync(NotFile file)
    {
        _notFileDbContext.NotFiles.Update(file);
        return Task.FromResult(true);
    }

    public async Task DeleteFileAsync(Guid fileId)
    {
        var data = await _notFileDbContext.NotFiles
            .FirstOrDefaultAsync(x => x.FileId.Equals(fileId));
        if (data is null)
            throw new NotFileException("文件不存在");
        _notFileDbContext.NotFiles.Remove(data);
    }

    /// <summary>
    /// 检查文件是否存在（仅活跃文件，排除已软删除的）。
    /// 使用 AnyAsync 避免加载整个实体到内存。
    /// </summary>
    public async Task<bool> FileExistsAsync(Guid fileId)
    {
        return await _notFileDbContext.NotFiles
            .AnyAsync(x => x.FileId.Equals(fileId) && !x.IsDeleted);
    }

    public async Task<long> GetFileCountByUserIdAsync(Guid userId)
    {
        var count =await _notFileDbContext.NotFiles
            .Where(x => x.UserId.Equals(userId) && !x.IsDeleted).CountAsync();
        return count;
    }

    public async Task<long> GetTotalFileSizeByUserIdAsync(Guid userId)
    {
        return await _notFileDbContext.NotFiles
            .Where(x => x.UserId.Equals(userId) && !x.IsDeleted)
            .SumAsync(x => x.FileSize);
    }

    /// <summary>
    /// 秒传查询：按 MD5 与大小匹配未删除文件，优先返回调用者自己的记录（F-09.1）
    /// </summary>
    public async Task<NotFile?> GetDeduplicateFileAsync(string fileMd5, long fileSize, Guid userId)
    {
        return await _notFileDbContext.NotFiles
            .AsNoTracking()
            .Where(f => f.FileMd5 == fileMd5 && f.FileSize == fileSize && !f.IsDeleted)
            .OrderByDescending(f => f.UserId == userId)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// 统计引用同一物理文件（FileUri）的其他活跃记录数（F-09.4，数据库端 Count）
    /// </summary>
    public async Task<int> CountActiveRefsByFileUriAsync(Uri fileUri, Guid excludeFileId)
    {
        return await _notFileDbContext.NotFiles
            .CountAsync(f => f.FileUri == fileUri && !f.IsDeleted && f.FileId != excludeFileId);
    }
}

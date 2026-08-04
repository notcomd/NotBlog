using FileDev.Domain.Entities;
using FileDev.Domain.IRepository;
using FileDev.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace FileDev.Infrastructure.Repository;

public class FileChunkRepository : IFileChunkRepository
{
    private readonly NotFileDbContext _context;

    public FileChunkRepository(NotFileDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public IUnitOfWork UnitOfWork => _context;

    /// <summary>
    /// 使用 AsNoTracking 读取：FileChunkManager.GetUploadStatusAsync 会修改返回的实体
    /// （合并 Redis 状态），AsNoTracking 避免修改被跟踪实体导致意外保存。
    /// 写路径（MarkChunkUploadedAsync / MarkMergedAsync / CancelUploadAsync）通过
    /// UpdateAsync 调用 Update(record) 显式附加实体，AsNoTracking 不影响写操作。
    /// </summary>
    public async Task<FileChunkRecord?> GetByFileKeyAsync(string fileKey, CancellationToken ct = default)
    {
        return await _context.Set<FileChunkRecord>()
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.FileKey == fileKey, ct)
            .ConfigureAwait(false);
    }

    public async Task InsertAsync(FileChunkRecord record, CancellationToken ct = default)
    {
        await _context.Set<FileChunkRecord>().AddAsync(record, ct).ConfigureAwait(false);
    }

    public Task UpdateAsync(FileChunkRecord record, CancellationToken ct = default)
    {
        _context.Set<FileChunkRecord>().Update(record);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(string fileKey, CancellationToken ct = default)
    {
        var record = await GetByFileKeyAsync(fileKey, ct).ConfigureAwait(false);
        if (record != null)
            _context.Set<FileChunkRecord>().Remove(record);
    }

    public async Task<IEnumerable<FileChunkRecord>> GetExpiredRecordsAsync(DateTimeOffset threshold,
        CancellationToken ct = default)
    {
        return await _context.Set<FileChunkRecord>()
            .AsNoTracking()
            .Where(r => r.Status != ChunkUploadStatus.Merged &&
                        r.Status != ChunkUploadStatus.Cancelled &&
                        r.CreatedAt < threshold)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }
}

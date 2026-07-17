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

    public async Task<FileChunkRecord?> GetByFileKeyAsync(string fileKey, CancellationToken ct = default)
    {
        return await _context.Set<FileChunkRecord>()
            .FirstOrDefaultAsync(r => r.FileKey == fileKey, ct)
            .ConfigureAwait(false);
    }

    public async Task InsertAsync(FileChunkRecord record, CancellationToken ct = default)
    {
        await _context.Set<FileChunkRecord>().AddAsync(record, ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(FileChunkRecord record, CancellationToken ct = default)
    {
        _context.Set<FileChunkRecord>().Update(record);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(string fileKey, CancellationToken ct = default)
    {
        var record = await GetByFileKeyAsync(fileKey, ct).ConfigureAwait(false);
        if (record != null)
            _context.Set<FileChunkRecord>().Remove(record);
    }

    public async Task<IEnumerable<FileChunkRecord>> GetExpiredRecordsAsync(DateTime threshold,
        CancellationToken ct = default)
    {
        return await _context.Set<FileChunkRecord>()
            .Where(r => r.Status != ChunkUploadStatus.Merged &&
                        r.Status != ChunkUploadStatus.Cancelled &&
                        r.CreatedAt < threshold)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }
}

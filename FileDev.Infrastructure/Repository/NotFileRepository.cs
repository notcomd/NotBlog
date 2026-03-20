using FileDev.Domain.Entities;
using FileDev.Domain.Exception;
using FileDev.Domain.IRepository;
using FileDev.Domain.SeedWork;
using FileDev.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace FileDev.Infrastructure.Repository;

public class NotFileRepository(NotFileDbContext notFileDbContext) : INotFileRepository
{
    private readonly NotFileDbContext _notFileDbContext =
        notFileDbContext ?? throw new ArgumentNullException(nameof(notFileDbContext));

    public IUnitOfWork UnitOfWork => notFileDbContext;

    public async Task<NotFile?> GetFileByIdAsync(Guid fileId)
    {
        return await _notFileDbContext.NotFiles
            .FirstOrDefaultAsync(x => x.FileId.Equals(fileId));
    }

    public async Task<IEnumerable<NotFile>?> GetAllFilesAsync()
    {
        return await _notFileDbContext.NotFiles
            .Where(en => !en.IsDeleted).ToListAsync();
    }

    public async Task<IEnumerable<NotFile>> GetFilesByUserIdAsync(Guid userId)
    {
        if (Guid.Empty == userId)
            throw new NotFileException("userId is null");
        return await _notFileDbContext.NotFiles
            .Where(x => x.UserId.Equals(userId))
            .ToListAsync();
    }

    public async Task<IEnumerable<NotFile>> GetPublicFilesAsync()
    {
        return await _notFileDbContext.NotFiles
            .Where(x => x.FileIdentity.Equals(FileIdentity.FilePublic) && !x.IsDeleted)
            .ToListAsync();
    }

    public async Task<IEnumerable<NotFile>?> GetFilesByTypeAsync(FileType fileType)
    {
        return await _notFileDbContext.NotFiles
            .Where(x => x.FileType.Equals(fileType) && !x.IsDeleted)
            .ToListAsync();
    }

    public async Task<IEnumerable<NotFile>?> GetFilesByTagsAsync(HashSet<string> tags)
    {
        return await _notFileDbContext.NotFiles
            .Where(x => x.FileTags.Intersect(tags).Any() && !x.IsDeleted)
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

    public async Task<bool> FileExistsAsync(Guid fileId)
    {
        var data =await _notFileDbContext.NotFiles
            .FirstOrDefaultAsync(x => x.FileId.Equals(fileId));
        return data != null;
    }

    public async Task<long> GetFileCountByUserIdAsync(Guid userId)
    {
        var count =await _notFileDbContext.NotFiles
            .Where(x => x.UserId.Equals(userId) && !x.IsDeleted).CountAsync();
        return count;
    }

    public async Task<double> GetTotalFileSizeByUserIdAsync(Guid userId)
    {
        return await _notFileDbContext.NotFiles
            .Where(x => x.UserId.Equals(userId) && !x.IsDeleted)
            .SumAsync(x => x.FileSize);
    }

    public async Task<IEnumerable<NotFile>?> FileByFileAllAsync(Guid userId)
    {
        return await _notFileDbContext
            .NotFiles
            .Where(en => en.UserId.Equals(userId) && !en.IsDeleted)
            .ToListAsync();
    }

    public Task<NotFile> FileByFileIdAsync(int id)
    {
        throw new NotImplementedException();
    }
}
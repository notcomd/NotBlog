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

    public Task<NotFile> GetFileByIdAsync(Guid fileId)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<NotFile>> GetAllFilesAsync()
    {
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<NotFile>> GetFilesByUserIdAsync(Guid userId)
    {
        if (Guid.Empty == userId)
            throw new NotFileException("userId is null");
        return await _notFileDbContext.Files
            .Where(x => x.UserId.Equals(userId))
            .ToListAsync();
    }

    public async Task<IEnumerable<NotFile>> GetPublicFilesAsync()
    {
        return await _notFileDbContext.Files
            .Where(x => x.FileIdentity.Equals(FileIdentity.FilePublic) && !x.IsDeleted)
            .ToListAsync();
    }

    public Task<IEnumerable<NotFile>> GetFilesByTypeAsync(FileType fileType)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<NotFile>> GetFilesByTagsAsync(HashSet<string> tags)
    {
        throw new NotImplementedException();
    }

    public Task<NotFile> AddFileAsync(NotFile file)
    {
        throw new NotImplementedException();
    }

    public Task<NotFile> UpdateFileAsync(NotFile file)
    {
        throw new NotImplementedException();
    }

    public Task DeleteFileAsync(Guid fileId)
    {
        throw new NotImplementedException();
    }

    public Task<bool> FileExistsAsync(Guid fileId)
    {
        throw new NotImplementedException();
    }

    public Task<long> GetFileCountByUserIdAsync(Guid userId)
    {
        throw new NotImplementedException();
    }

    public Task<double> GetTotalFileSizeByUserIdAsync(Guid userId)
    {
        throw new NotImplementedException();
    }

    public Task<NotFile> FileByFileAllAsync()
    {
        throw new NotImplementedException();
    }

    public Task<NotFile> FileByFileIdAsync(int id)
    {
        throw new NotImplementedException();
    }
}
using FileDev.Domain.Entities;
using FileDev.Domain.SeedWork;
namespace FileDev.Domain.IRepository;

public interface INotFileRepository:IRepository<NotFile>
{
    Task<NotFile?> GetFileByIdAsync(Guid fileId);

    Task<IEnumerable<NotFile>?> GetAllFilesAsync();

    Task<IEnumerable<NotFile>> GetFilesByUserIdAsync(Guid userId);

    Task<IEnumerable<NotFile>> GetPublicFilesAsync();

    Task<IEnumerable<NotFile>?> GetFilesByTypeAsync(FileType fileType);

    Task<IEnumerable<NotFile>?> GetFilesByTagsAsync(HashSet<string> tags);

    Task InsertFileAsync(NotFile file);

    Task<bool> UpdateFileAsync(NotFile file);

    Task DeleteFileAsync(Guid fileId);

    Task<bool> FileExistsAsync(Guid fileId);

    Task<long> GetFileCountByUserIdAsync(Guid userId);

    Task<double> GetTotalFileSizeByUserIdAsync(Guid userId);
}
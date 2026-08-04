using FileDev.Domain.Entities;
using FileDev.Domain.SeedWork;
namespace FileDev.Domain.IRepository;

public interface INotFileRepository:IRepository<NotFile>
{
    Task<NotFile?> GetFileByIdAsync(Guid fileId);

    Task<IEnumerable<NotFile>?> GetAllFilesAsync();

    Task<IEnumerable<NotFile>> GetFilesByUserIdAsync(Guid userId);

    Task<IEnumerable<NotFile>> GetPublicFilesAsync();

   // Task<IEnumerable<NotFile>?> GetFilesByTypeAsync(FileType fileType);

    Task<IEnumerable<NotFile>?> GetFilesByTagsAsync(HashSet<string> tags);

    Task InsertFileAsync(NotFile file);

    Task<bool> UpdateFileAsync(NotFile file);

    Task DeleteFileAsync(Guid fileId);

    Task<bool> FileExistsAsync(Guid fileId);

    Task<long> GetFileCountByUserIdAsync(Guid userId);

    Task<long> GetTotalFileSizeByUserIdAsync(Guid userId);

    /// <summary>
    /// 秒传查询：按 MD5 与大小匹配未删除文件，优先返回调用者自己的记录（F-09.1）
    /// </summary>
    /// <param name="fileMd5">文件 MD5</param>
    /// <param name="fileSize">文件大小（MD5 相同但大小不同不应命中秒传）</param>
    /// <param name="userId">当前调用者</param>
    Task<NotFile?> GetDeduplicateFileAsync(string fileMd5, long fileSize, Guid userId);

    /// <summary>
    /// 统计引用同一物理文件（FileUri）的其他活跃记录数，用于物理文件清理判断（F-09.4）
    /// </summary>
    /// <param name="fileUri">物理文件 URI</param>
    /// <param name="excludeFileId">排除当前文件（自身不参与计数）</param>
    Task<int> CountActiveRefsByFileUriAsync(Uri fileUri, Guid excludeFileId);
}

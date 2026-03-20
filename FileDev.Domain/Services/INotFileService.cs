using FileDev.Domain.Entities;

namespace FileDev.Domain.Services;

public interface INotFileService
{
    /// <summary>
    ///  创建文件
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="fileName"></param>
    /// <param name="fileTags"></param>
    /// <param name="fileDescription"></param>
    /// <param name="fileType"></param>
    /// <param name="fileSize"></param>
    /// <param name="fileUri"></param>
    /// <param name="fileMd5"></param>
    /// <param name="fileIdentity"></param>
    /// <returns></returns>
    Task CreateFileAsync(Guid userId, string fileName, HashSet<string>? fileTags,
        string fileDescription, FileType fileType, double fileSize, Uri fileUri, string fileMd5,
        FileIdentity fileIdentity = FileIdentity.FilePrivate);
    
    /// <summary>
    ///  获取用户所有文件
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    Task<IEnumerable<NotFile>> GetFilesByUserIdAsync(Guid userId);
    
    /// <summary>
    ///  获取文件
    /// </summary>
    /// <param name="fileId"></param>
    /// <returns></returns>
    Task<NotFile> GetFileByIdAsync(Guid fileId);
    
    /// <summary>
    ///  更新文件
    /// </summary>
    /// <param name="fileId"></param>
    /// <param name="fileName"></param>
    /// <param name="fileTags"></param>
    /// <param name="fileDescription"></param>
    /// <param name="fileIdentity"></param>
    /// <param name="fileMd5"></param>
    /// <returns></returns>
    Task UpdateFileAsync(Guid fileId, string fileName, HashSet<string>? fileTags,
        string fileDescription, FileIdentity fileIdentity, string fileMd5);
    
    /// <summary>
    ///  删除文件
    /// </summary>
    /// <param name="fileId"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    Task DeleteFileAsync(Guid fileId, Guid userId);
    
}
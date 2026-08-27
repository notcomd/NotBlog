using FileDev.Domain.Entities;
using FileDev.Domain.Options;

namespace FileDev.Domain.IServices;

public interface INotFileService
{
    /// <summary>
    ///  校验上传前置条件（S-09 配额 / S-17 内容类型 / 大小上限）：
    ///  用户标识、文件名、扩展名白名单、文件大小上限、用户存储配额。
    ///  任一不满足即抛出对应业务异常，不做任何数据变更。
    /// </summary>
    /// <param name="userId">当前用户</param>
    /// <param name="fileName">文件名（用于扩展名白名单校验）</param>
    /// <param name="fileSize">文件字节数</param>
    /// <param name="options">文件存储配置（白名单、大小上限、配额）</param>
    Task ValidateUploadAsync(Guid userId, string fileName, long fileSize,
        NotFileStorageOptions options, CancellationToken cancellationToken = default);

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
    /// <returns>落库文件实体（含真实 FileId，供上层响应映射使用）</returns>
    Task<NotFile> CreateFileAsync(Guid userId, string fileName, HashSet<string>? fileTags,
        string fileDescription, FileType fileType, long fileSize, Uri fileUri, string fileMd5,
        FileIdentity fileIdentity = FileIdentity.FilePrivate,
        NotFileStorageResponse? storageMeta = null, FileSource source = FileSource.UserRepository);

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
    Task<NotFile?> GetFileByIdAsync(Guid fileId);

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
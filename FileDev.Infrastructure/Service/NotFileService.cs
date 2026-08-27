using FileDev.Domain.Entities;
using FileDev.Domain.Exception;
using FileDev.Domain.IRepository;
using FileDev.Domain.IServices;
using FileDev.Domain.Options;
using Microsoft.Extensions.Logging;

namespace FileDev.Infrastructure.Service;

public class NotFileService(INotFileRepository notFileRepository, ILogger<INotFileService> logger) : INotFileService
{
    /// <summary>
    /// 上传前置条件统一校验（S-09 配额 / S-17 内容类型 / 大小上限）。
    /// 收敛 UploadFile / StreamUpload / ChunkUploadInit 三条上传链路的重复校验逻辑。
    /// </summary>
    public async Task ValidateUploadAsync(Guid userId, string fileName, long fileSize,
        NotFileStorageOptions options, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("用户ID不能为空");
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("文件名不能为空");

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        // 白名单为空时跳过校验（与各上传链路既有行为保持一致）
        if (options.AllowedExtensions is { Count: > 0 } && !options.AllowedExtensions.Contains(ext))
        {
            logger.LogWarning("Extension not allowed: FileName={FileName}, Ext={Ext}", fileName, ext);
            throw new ArgumentException($"不支持的文件类型: {ext}");
        }

        if (fileSize > options.MaxFileSize)
            throw new ArgumentException($"文件大小超过限制 {options.MaxFileSize / 1024 / 1024}MB");

        // S-09：写入前配额检查
        var used = await notFileRepository.GetTotalFileSizeByUserIdAsync(userId);
        if (used + fileSize > options.UserStorageQuota)
        {
            logger.LogWarning("Storage quota exceeded: UserId={UserId}, Used={Used}, New={New}",
                userId, used, fileSize);
            throw new FileQuotaExceededException("用户存储配额不足");
        }
    }

    public async Task<NotFile> CreateFileAsync(Guid userId, string fileName, HashSet<string>? fileTags,
        string? fileDescription,
        FileType fileType,
        long fileSize, Uri fileUri, string fileMd5, FileIdentity fileIdentity = FileIdentity.FilePrivate,
        NotFileStorageResponse? storageMeta = null, FileSource source = FileSource.UserRepository)
    {
        var builder = new NotFile.NotFileBuilder()
            .WithFileName(fileName)
            .WithFileTags(fileTags ?? [])
            .WithFileDescription(fileDescription ?? string.Empty)
            .WithFileSize(fileSize)
            .WithFileUri(fileUri)
            .WithFileMd5(fileMd5)
            .WithFileIdentity(fileIdentity)
            .WithSource(source)
            .WithUserId(userId);

        // 对齐 Lite 存储元数据：由存储写回（内容哈希 / 存储层 / 卷 / 分片数 / 过期时间）
        if (storageMeta is { Success: true })
        {
            builder.WithStorageMeta(
                storageMeta.ContentHash,
                storageMeta.Tier,
                storageMeta.VolumeId,
                storageMeta.ShardCount,
                storageMeta.ExpiresAt,
                storageMeta.UpdatedUtc);
        }

        var file = builder.Build();
        await notFileRepository.InsertFileAsync(file);
        // 返回落库实体（FileId 在实体构造时生成），上层无需再反查即可获取真实 FileId
        return file;
    }

    public async Task<IEnumerable<NotFile>> GetFilesByUserIdAsync(Guid userId)
    {
        var fileData = await notFileRepository.GetFilesByUserIdAsync(userId);
        return fileData.Where(en => !en.IsDeleted);
    }

    public async Task<NotFile?> GetFileByIdAsync(Guid fileId)
    {
        var fileData = await notFileRepository.GetFileByIdAsync(fileId);
        if (fileData is { IsDeleted: true })
        {
            // 可预期的业务性失败（文件已被删除），用 Warning 而非 Error
            logger.LogWarning("File not found {FileId}", fileId);
            return null;
        }

        return fileData;
    }

    public async Task UpdateFileAsync(Guid fileId, string fileName, HashSet<string>? fileTags, string fileDescription,
        FileIdentity fileIdentity,
        string fileMd5)
    {
        var file = await GetFileByIdAsync(fileId);
        if (file is null)
        {
            // 不再静默吞错：文件不存在属于可预期业务性失败，记录警告并抛异常交由上层统一处理
            logger.LogWarning("File not found {FileId}", fileId);
            throw new NotFileNotFoundException($"文件不存在: {fileId}");
        }

        file.ChangeFileData(fileName, fileTags, fileDescription, fileIdentity, fileMd5);
        await notFileRepository.UpdateFileAsync(file);
    }

    /// <summary>
    /// 删除文件
    /// </summary>
    public async Task DeleteFileAsync(Guid fileId, Guid userId)
    {
        var file = await GetFileByIdAsync(fileId);
        if (file is null)
        {
            // 不再静默吞错：文件不存在属于可预期业务性失败，记录警告并抛异常交由上层统一处理
            logger.LogWarning("File not found {FileId}", fileId);
            throw new NotFileNotFoundException($"文件不存在: {fileId}");
        }

        if (file.UserId != userId)
        {
            // 越权删除属于业务性拒绝，用 Warning 级别记录，并抛异常交由上层统一处理
            logger.LogWarning("User not authorized to delete file {FileId}",
                          fileId);
            throw new FilePermissionDeniedException($"无权删除文件: {fileId}");
        }

        file.SoftDelete();
        await notFileRepository.UpdateFileAsync(file);
        logger.LogInformation("File deleted {FileId}",
                              fileId);
    }
}
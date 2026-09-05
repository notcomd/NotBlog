using FileDev.Domain.Entities;
using FileDev.Domain.Exception;
using FileDev.Domain.IRepository;
using FileDev.Domain.IServices;
using FileDev.Domain.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FileDev.Infrastructure.Service;

/// <summary>
/// 文件元数据服务：统一上传前置校验、文件 CRUD 与用户存储额度记账。
/// 配额基于 UserFileInfo 表（每用户一条记录），校验/占用/释放与文件写入同一事务，
/// 由数据库端原子 UPDATE 保证并发正确性（详见 <see cref="IUserFileInfoRepository"/>）。
/// </summary>
public class NotFileService(
    INotFileRepository notFileRepository,
    IUserFileInfoRepository userFileInfoRepository,
    IOptionsSnapshot<NotFileStorageOptions> configOptions,
    ILogger<INotFileService> logger) : INotFileService
{
    private readonly INotFileRepository _notFileRepository =
        notFileRepository ?? throw new ArgumentNullException(nameof(notFileRepository));
    private readonly IUserFileInfoRepository _userFileInfoRepository =
        userFileInfoRepository ?? throw new ArgumentNullException(nameof(userFileInfoRepository));
    private readonly NotFileStorageOptions _config =
        configOptions?.Value ?? throw new ArgumentNullException(nameof(configOptions));
    private readonly ILogger<INotFileService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// 上传前置条件统一校验（S-09 配额 / S-17 内容类型 / 大小上限）。
    /// 收敛 UploadFile / StreamUpload / ChunkUploadInit 三条上传链路的重复校验逻辑。
    /// 配额校验读取 UserFileInfo 记账表（缺失时惰性初始化，初始已用量以文件表聚合兜底），
    /// 不做数据变更。
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
            _logger.LogWarning("Extension not allowed: FileName={FileName}, Ext={Ext}", fileName, ext);
            throw new ArgumentException($"不支持的文件类型: {ext}");
        }

        if (fileSize > options.MaxFileSize)
            throw new ArgumentException($"文件大小超过限制 {options.MaxFileSize / 1024 / 1024}MB");

        // S-09：写入前配额检查（UserFileInfo 记账表，缺失记录惰性初始化）
        var quota = await GetOrEnsureQuotaAsync(userId, options.UserStorageQuota, cancellationToken);
        if (quota.TotalQuotaBytes > 0 && quota.UsedBytes + fileSize > quota.TotalQuotaBytes)
        {
            _logger.LogWarning("Storage quota exceeded: UserId={UserId}, Used={Used}, New={New}",
                userId, quota.UsedBytes, fileSize);
            throw new FileQuotaExceededException("用户存储配额不足");
        }
    }

    /// <summary>
    /// 上传成功后占用额度（原子 UPDATE，配额不足抛 <see cref="FileQuotaExceededException"/>）。
    /// 与文件元数据创建在同一请求事务内提交，回滚时一并回退。
    /// </summary>
    /// <param name="userId">用户 ID。</param>
    /// <param name="bytes">本次写入文件字节数。</param>
    public async Task OccupyQuotaAsync(Guid userId, long bytes, CancellationToken cancellationToken = default)
    {
        if (bytes <= 0)
            return;

        // 记录缺失时先惰性初始化（与 ValidateUploadAsync 行为一致）
        await GetOrEnsureQuotaAsync(userId, _config.UserStorageQuota, cancellationToken);
        var occupied = await _userFileInfoRepository.TryOccupyAsync(userId, bytes, cancellationToken);
        if (!occupied)
        {
            _logger.LogWarning("Storage quota occupy failed: UserId={UserId}, Bytes={Bytes}", userId, bytes);
            throw new FileQuotaExceededException("用户存储配额不足");
        }
    }

    /// <summary>
    /// 删除文件后释放占用额度（原子 UPDATE，结果不低于 0；记录不存在时忽略）。
    /// </summary>
    /// <param name="userId">用户 ID。</param>
    /// <param name="bytes">释放字节数（即被删除文件的 FileSize）。</param>
    public async Task ReleaseQuotaAsync(Guid userId, long bytes, CancellationToken cancellationToken = default)
    {
        if (bytes <= 0)
            return;
        await _userFileInfoRepository.ReleaseAsync(userId, bytes, cancellationToken);
    }

    /// <summary>读取用户额度记录；缺失时惰性初始化（存量用户初始已用量以文件表聚合兜底）。</summary>
    private async Task<UserFileInfo> GetOrEnsureQuotaAsync(Guid userId, long defaultQuota,
        CancellationToken cancellationToken)
    {
        var existing = await _userFileInfoRepository.GetByUserIdAsync(userId);
        if (existing is not null)
            return existing;

        // 存量用户兜底：初始已用量 = 文件表实时聚合值（新用户为 0）
        var used = await _notFileRepository.GetTotalFileSizeByUserIdAsync(userId);
        return await _userFileInfoRepository.EnsureUserFileInfoAsync(
            new UserFileInfo(userId, defaultQuota, used));
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
        await _notFileRepository.InsertFileAsync(file);
        // 返回落库实体（FileId 在实体构造时生成），上层无需再反查即可获取真实 FileId
        return file;
    }

    public async Task<IEnumerable<NotFile>> GetFilesByUserIdAsync(Guid userId)
    {
        var fileData = await _notFileRepository.GetFilesByUserIdAsync(userId);
        return fileData.Where(en => !en.IsDeleted);
    }

    public async Task<NotFile?> GetFileByIdAsync(Guid fileId)
    {
        var fileData = await _notFileRepository.GetFileByIdAsync(fileId);
        if (fileData is { IsDeleted: true })
        {
            // 可预期的业务性失败（文件已被删除），用 Warning 而非 Error
            _logger.LogWarning("File not found {FileId}", fileId);
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
            _logger.LogWarning("File not found {FileId}", fileId);
            throw new NotFileNotFoundException($"文件不存在: {fileId}");
        }

        file.ChangeFileData(fileName, fileTags, fileDescription, fileIdentity, fileMd5);
        await _notFileRepository.UpdateFileAsync(file);
    }

    /// <summary>
    /// 删除文件（软删），并同步释放该文件占用的存储额度。
    /// </summary>
    public async Task DeleteFileAsync(Guid fileId, Guid userId, CancellationToken cancellationToken = default)
    {
        var file = await GetFileByIdAsync(fileId);
        if (file is null)
        {
            // 不再静默吞错：文件不存在属于可预期业务性失败，记录警告并抛异常交由上层统一处理
            _logger.LogWarning("File not found {FileId}", fileId);
            throw new NotFileNotFoundException($"文件不存在: {fileId}");
        }

        if (file.UserId != userId)
        {
            // 越权删除属于业务性拒绝，用 Warning 级别记录，并抛异常交由上层统一处理
            _logger.LogWarning("User not authorized to delete file {FileId}",
                          fileId);
            throw new FilePermissionDeniedException($"无权删除文件: {fileId}");
        }

        file.SoftDelete();
        await _notFileRepository.UpdateFileAsync(file);
        // 软删后文件不再计入用量，同步释放额度（与文件软删同一事务提交）
        await ReleaseQuotaAsync(userId, file.FileSize, cancellationToken);
        _logger.LogInformation("File deleted {FileId}",
                              fileId);
    }
}

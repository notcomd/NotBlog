namespace FileDev.Web.API.Application.Command;

public class ChunkUploadInitCommandHandler(
    IFileChunkManager chunkManager,
    FileDev.Domain.IRepository.INotFileRepository notFileRepository,
    IOptionsSnapshot<NotFileStorageOptions> configOptions,
    ILogger<ChunkUploadInitCommandHandler> logger)
    : NotMediator.IRequestHandler<ChunkUploadInitCommand, FileChunkRecord>
{
    private readonly NotFileStorageOptions _config =
        configOptions.Value ?? throw new ArgumentNullException(nameof(configOptions));

    public async Task<FileChunkRecord> Handler(ChunkUploadInitCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId == Guid.Empty)
            throw new ArgumentException("用户ID不能为空");
        if (string.IsNullOrWhiteSpace(request.FileName))
            throw new ArgumentException("文件名不能为空");
        if (request.TotalSize <= 0)
            throw new ArgumentException("文件大小必须大于0");
        if (request.ChunkSize <= 0)
            throw new ArgumentException("分片大小必须大于0");
        if (request.TotalSize > _config.MaxFileSize)
            throw new ArgumentException($"文件大小超过限制 {_config.MaxFileSize / 1024 / 1024}MB");

        // S-09：写入前配额检查
        var used = await notFileRepository.GetTotalFileSizeByUserIdAsync(request.UserId);
        if (used + request.TotalSize > _config.UserStorageQuota)
            throw new InvalidOperationException("用户存储配额不足");

        var ext = Path.GetExtension(request.FileName).ToLowerInvariant();
        logger.LogDebug("[ChunkInit] 文件名校验: FileName={FileName}, Ext={Ext}, WhitelistCount={Count}",
            request.FileName, ext, _config.AllowedExtensions.Count);

        if (_config.AllowedExtensions is { Count: > 0 })
        {
            if (!_config.AllowedExtensions.Contains(ext))
            {
                logger.LogWarning("[ChunkInit] 拒绝 — 扩展名不在白名单: FileName={FileName}, Ext={Ext}",
                    request.FileName, ext);
                throw new ArgumentException($"不支持的文件类型: {ext}");
            }

            logger.LogInformation("[ChunkInit] 白名单校验通过: FileName={FileName}, Ext={Ext}",
                request.FileName, ext);
        }
        else
        {
            logger.LogDebug("[ChunkInit] 白名单为空，跳过扩展名校验: FileName={FileName}", request.FileName);
        }

        // fileKey 统一为 {userId:N}/{guid:N}{ext}，合并后物理路径与下载 URI（/files/{fileKey}）一一对应
        var fileKey = $"{request.UserId:N}/{Guid.CreateVersion7():N}{ext}";

        var totalChunks = (int)Math.Ceiling((double)request.TotalSize / request.ChunkSize);

        var record = await chunkManager.InitializeUploadAsync(
            fileKey, request.UserId, request.FileName,
            request.TotalSize, request.ChunkSize, totalChunks,
            request.FileMd5, request.FileType, request.FileIdentity,
            request.FileTags, request.FileDescription, cancellationToken);

        logger.LogInformation("[ChunkUploadInit] 分片上传任务已初始化: FileKey={FileKey}, TotalChunks={Total}",
            fileKey, totalChunks);

        return record;
    }
}

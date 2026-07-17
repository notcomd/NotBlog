namespace FileDev.Web.API.Application.Command;

public class ChunkUploadInitCommandHandler(
    IFileChunkManager chunkManager,
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

        var ext = Path.GetExtension(request.FileName).ToLowerInvariant();
        if (!_config.AllowedExtensions.Contains(ext) && _config.AllowedExtensions is { Length: > 0 })
            throw new ArgumentException($"不支持的文件类型: {ext}");

        var safeName = request.FileName.Replace(" ", "_")
            .Replace("\\", "_").Replace("/", "_");

        var fileKey =
            $"{request.UserId:N}_{DateTimeOffset.UtcNow:yyyyMMddHHmmss}_{safeName}";

        var totalChunks = (int)Math.Ceiling((double)request.TotalSize / request.ChunkSize);

        var record = await chunkManager.InitializeUploadAsync(
            fileKey, request.UserId, request.FileName,
            request.TotalSize, request.ChunkSize, totalChunks,
            request.FileMd5, request.FileType, request.FileIdentity,
            request.FileTags, request.FileDescription, cancellationToken);

        logger.LogInformation("[ChunkInit] 分片上传任务已初始化: FileKey={FileKey}, TotalChunks={Total}",
            fileKey, totalChunks);

        return record;
    }
}

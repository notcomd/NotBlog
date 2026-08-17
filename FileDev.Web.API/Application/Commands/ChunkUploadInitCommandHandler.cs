namespace FileDev.Web.API.Application.Commands;

public class ChunkUploadInitCommandHandler(
    IFileChunkManager chunkManager,
    INotFileService notFileService,
    IOptionsSnapshot<NotFileStorageOptions> configOptions,
    ILogger<ChunkUploadInitCommandHandler> logger)
    :  IRequestHandler<ChunkUploadInitCommand, FileChunkRecord>
{
    private readonly NotFileStorageOptions _config =
        configOptions.Value ?? throw new ArgumentNullException(nameof(configOptions));

    public async Task<FileChunkRecord> Handler(ChunkUploadInitCommand request, CancellationToken cancellationToken)
    {
        if (request.TotalSize <= 0)
            throw new ArgumentException("文件大小必须大于0");
        if (request.ChunkSize <= 0)
            throw new ArgumentException("分片大小必须大于0");

        // 统一前置校验（S-09/S-17：扩展名白名单、大小上限、用户配额）
        await notFileService.ValidateUploadAsync(
            request.UserId, request.FileName, request.TotalSize, _config, cancellationToken);

        var ext = Path.GetExtension(request.FileName).ToLowerInvariant();

        // Major：路径拼接统一收敛至 FileApiHelpers.BuildFileKey
        var fileKey = FileApiHelpers.BuildFileKey(request.UserId, ext);

        var totalChunks = request.TotalChunks > 0
            ? request.TotalChunks
            : (int)Math.Ceiling((double)request.TotalSize / request.ChunkSize);

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

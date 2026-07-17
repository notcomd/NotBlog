namespace FileDev.Web.API.Application.Command;

public class UploadChunkCommandHandler(
    INotFileStorageService storageService,
    IFileChunkManager chunkManager,
    ILogger<UploadChunkCommandHandler> logger)
    : NotMediator.IRequestHandler<UploadChunkCommand, bool>
{
    public async Task<bool> Handler(UploadChunkCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FileKey))
            throw new ArgumentException("FileKey不能为空");
        if (request.ChunkContent == null || request.ChunkContent.Length == 0)
            throw new ArgumentException("分片内容不能为空");

        var result = await storageService.UploadChunkAsync(
            request.FileKey, request.ChunkIndex, request.ChunkContent,
            request.ChunkHash);

        if (!result.Success)
            throw new InvalidOperationException($"分片{request.ChunkIndex}上传失败: {result.ErrorMessage}");

        await chunkManager.MarkChunkUploadedAsync(request.FileKey, request.ChunkIndex, cancellationToken);

        logger.LogDebug("[ChunkUpload] 分片已上传: FileKey={FileKey}, Chunk={Chunk}", request.FileKey, request.ChunkIndex);
        return true;
    }
}

namespace FileDev.Web.API.Application.Command;

public class CancelChunksCommandHandler(
    IFileChunkManager chunkManager,
    ILogger<CancelChunksCommandHandler> logger)
    : NotMediator.IRequestHandler<CancelChunksCommand, bool>
{
    public async Task<bool> Handler(CancelChunksCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FileKey))
            throw new ArgumentException("FileKey不能为空");
        await chunkManager.CancelUploadAsync(request.FileKey, cancellationToken);
        logger.LogInformation("[ChunkUploadCancel] 上传已取消: FileKey={FileKey}", request.FileKey);
        return true;
    }
}

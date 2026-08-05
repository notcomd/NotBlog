namespace FileDev.Web.API.Application.Commands;

public class CancelChunksCommandHandler(
    IFileChunkManager chunkManager,
    INotFileStorageService storageService,
    ILogger<CancelChunksCommandHandler> logger)
    : NotMediator.IRequestHandler<CancelChunksCommand, bool>
{
    public async Task<bool> Handler(CancelChunksCommand request, 
    CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FileKey))
            throw new ArgumentException("FileKey不能为空");
        if (request.UserId == Guid.Empty)
            throw new ArgumentException("用户ID不能为空");

        // S-08：仅上传任务所有者可取消
        var record = await chunkManager.GetUploadStatusAsync(request.FileKey, cancellationToken);
        if (record == null)
            throw new NotFileNotFoundException($"未找到上传任务: {request.FileKey}");
        if (record.UserId != request.UserId)
            throw new FilePermissionDeniedException("无权操作此上传任务");

        await chunkManager.CancelUploadAsync(request.FileKey, cancellationToken);
        // S-09：取消时清理临时分片文件
        await storageService.CleanupChunksAsync(request.FileKey);
        logger.LogInformation("[ChunkUploadCancel] 上传已取消: FileKey={FileKey}", request.FileKey);
        return true;
    }
}

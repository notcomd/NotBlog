namespace FileDev.Web.API.Application.Command;

public class UploadChunkCommandHandler(
    INotFileStorageService storageService,
    IFileChunkManager chunkManager,
    IOptionsSnapshot<NotFileStorageOptions> configOptions,
    ILogger<UploadChunkCommandHandler> logger)
    : NotMediator.IRequestHandler<UploadChunkCommand, bool>
{
    public async Task<bool> Handler(UploadChunkCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FileKey))
            throw new ArgumentException("FileKey不能为空");
        if (request.ChunkContent == null || request.ChunkContent.Length == 0)
            throw new ArgumentException("分片内容不能为空");
        if (request.UserId == Guid.Empty)
            throw new ArgumentException("用户ID不能为空");

        // S-09：分片大小上限校验（防御，API 层已先校验）
        if (request.ChunkContent.Length > configOptions.Value.ChunkFileSize * 2)
            throw new ArgumentException("分片数据超出大小限制");

        // S-08：分片归属校验前置——仅上传任务所有者可上传分片
        var record = await chunkManager.GetUploadStatusAsync(request.FileKey, cancellationToken);
        if (record == null)
            throw new InvalidOperationException($"未找到上传任务: {request.FileKey}");
        if (record.UserId != request.UserId)
            throw new UnauthorizedAccessException("无权操作此上传任务");
        if (record.Status == ChunkUploadStatus.Merged
            || record.Status == ChunkUploadStatus.Cancelled)
            throw new InvalidOperationException("上传任务已结束，无法继续上传");

        // 分片索引合法性校验（越界直接拒绝，避免写盘脏数据）
        if (request.ChunkIndex < 0 || request.ChunkIndex >= record.TotalChunks)
            throw new ArgumentOutOfRangeException(nameof(request.ChunkIndex),
                $"分片索引 {request.ChunkIndex} 超出范围 [0, {record.TotalChunks - 1}]");

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

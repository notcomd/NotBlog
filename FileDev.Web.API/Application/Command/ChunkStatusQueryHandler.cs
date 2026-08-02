namespace FileDev.Web.API.Application.Command;

public class ChunkStatusQueryHandler(
    IFileChunkManager chunkManager)
    : NotMediator.IRequestHandler<ChunkStatusQuery, ChunkStatusResponse>
{
    public async Task<ChunkStatusResponse> Handler(ChunkStatusQuery request, CancellationToken cancellationToken)
    {
        var record = await chunkManager.GetUploadStatusAsync(request.FileKey, cancellationToken);
        if (record == null)
            throw new InvalidOperationException($"未找到上传任务: {request.FileKey}");

        // S-08：仅上传任务所有者可查询状态
        if (record.UserId != request.UserId)
            throw new UnauthorizedAccessException("无权操作此上传任务");

        var uploadedChunks = await chunkManager.GetUploadedChunksAsync(request.FileKey, cancellationToken);

        return new ChunkStatusResponse
        {
            FileKey = record.FileKey,
            TotalChunks = record.TotalChunks,
            UploadedChunks = uploadedChunks,
            IsComplete = uploadedChunks.Count >= record.TotalChunks,
            Status = record.Status
        };
    }
}

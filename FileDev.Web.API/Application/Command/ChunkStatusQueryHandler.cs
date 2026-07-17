namespace FileDev.Web.API.Application.Command;

public class ChunkStatusQueryHandler(
    IFileChunkManager chunkManager,
    ILogger<ChunkStatusQueryHandler> logger)
    : NotMediator.IRequestHandler<ChunkStatusQuery, ChunkStatusResponse>
{
    public async Task<ChunkStatusResponse> Handler(ChunkStatusQuery request, CancellationToken cancellationToken)
    {
        var record = await chunkManager.GetUploadStatusAsync(request.FileKey, cancellationToken);
        if (record == null)
            throw new InvalidOperationException($"未找到上传任务: {request.FileKey}");

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

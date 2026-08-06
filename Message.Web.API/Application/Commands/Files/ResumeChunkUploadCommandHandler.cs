
namespace Message.Web.API.Application.Commands.Files;
/// <summary>
/// 断点续传命令处理程序。
/// </summary>
public class ResumeChunkUploadCommandHandler(
    IFileStorageGrpcClient fileStorage,
    ILogger<ResumeChunkUploadCommandHandler> logger) : IRequestHandler<ResumeChunkUploadCommand, ChunkStatusResult>
{
    public async Task<ChunkStatusResult> Handler(ResumeChunkUploadCommand command, CancellationToken cancellationToken)
    {
        var result = await fileStorage.ResumeChunkUploadAsync(
            command.FileKey,
            command.TotalChunks,
            command.ChunkSize,
            command.Chunks,
            null,
            cancellationToken);

        logger.LogInformation("断点续传：FileKey={FileKey}，成功={Success}，已上传={UploadedChunks}/{TotalChunks}",
            command.FileKey, result.Success, result.UploadedChunks.Count, result.TotalChunks);
        return result;
    }
}

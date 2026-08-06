
namespace Message.Web.API.Application.Commands.Files;
/// <summary>
/// 上传单个分片命令处理程序。
/// </summary>
public class UploadChunkCommandHandler(
    IFileStorageGrpcClient fileStorage,
    ILogger<UploadChunkCommandHandler> logger) : IRequestHandler<UploadChunkCommand, ChunkUploadResult>
{
    public async Task<ChunkUploadResult> Handler(UploadChunkCommand command, CancellationToken cancellationToken)
    {
        var result = await fileStorage.UploadChunkAsync(
            command.FileKey,
            command.ChunkIndex,
            command.ChunkData,
            command.ChunkMd5,
            cancellationToken);

        logger.LogInformation("上传分片：FileKey={FileKey}，索引={ChunkIndex}，成功={Success}",
            command.FileKey, command.ChunkIndex, result.Success);
        return result;
    }
}

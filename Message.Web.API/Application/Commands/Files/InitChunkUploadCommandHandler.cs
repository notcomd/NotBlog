
namespace Message.Web.API.Application.Commands.Files;
/// <summary>
/// 初始化分片上传命令处理程序。
/// </summary>
public class InitChunkUploadCommandHandler(
    IFileStorageGrpcClient fileStorage,
    ILogger<InitChunkUploadCommandHandler> logger) : IRequestHandler<InitChunkUploadCommand, ChunkUploadInitResult>
{
    public async Task<ChunkUploadInitResult> Handler(InitChunkUploadCommand command, CancellationToken cancellationToken)
    {
        var result = await fileStorage.InitChunkUploadAsync(
            command.UserId,
            command.FileName,
            command.TotalSize,
            command.FileMd5,
            command.Description,
            command.IsPublic,
            cancellationToken);

        logger.LogInformation("初始化分片上传：FileKey={FileKey}，成功={Success}",
            result.FileKey, result.Success);
        return result;
    }
}

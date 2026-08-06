
namespace Message.Web.API.Application.Commands.Files;
/// <summary>
/// 取消分片上传命令处理程序。
/// </summary>
public class CancelChunkUploadCommandHandler(
    IFileStorageGrpcClient fileStorage,
    ILogger<CancelChunkUploadCommandHandler> logger) : IRequestHandler<CancelChunkUploadCommand, CancelChunkUploadResult>
{
    public async Task<CancelChunkUploadResult> Handler(CancelChunkUploadCommand command, CancellationToken cancellationToken)
    {
        var result = await fileStorage.CancelChunkUploadAsync(command.FileKey, cancellationToken);
        logger.LogInformation("取消分片上传：FileKey={FileKey}，成功={Success}", command.FileKey, result.Success);
        return result;
    }
}

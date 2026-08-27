
namespace Message.Web.API.Application.Commands.Files;
/// <summary>
/// 合并分片命令处理程序。
/// </summary>
public class MergeChunksCommandHandler(
    IFileStorageGrpcClient fileStorage,
    ILogger<MergeChunksCommandHandler> logger) : IRequestHandler<MergeChunksCommand, MergeChunksResult>
{
    public async Task<MergeChunksResult> Handler(MergeChunksCommand command, CancellationToken cancellationToken)
    {
        var result = await fileStorage.MergeChunksAsync(
            command.FileKey,
            command.UserId,
            command.FileName,
            command.Description,
            command.ContentId,
            command.ContentType,
            cancellationToken);

        logger.LogInformation("合并分片：FileKey={FileKey}，成功={Success}，文件ID={FileId}",
            command.FileKey, result.Success, result.FileId);
        return result;
    }
}

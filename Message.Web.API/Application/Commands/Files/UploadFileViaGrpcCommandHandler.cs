
namespace Message.Web.API.Application.Commands.Files;
/// <summary>
/// 小文件上传命令处理程序。
/// </summary>
public class UploadFileViaGrpcCommandHandler(
    IFileStorageGrpcClient fileStorage,
    ILogger<UploadFileViaGrpcCommandHandler> logger) : IRequestHandler<UploadFileViaGrpcCommand, UploadFileResult>
{
    public async Task<UploadFileResult> Handler(UploadFileViaGrpcCommand command, CancellationToken cancellationToken)
    {
        var result = await fileStorage.UploadFileAsync(
            command.UserId,
            command.FileName,
            command.Content,
            command.Description,
            expectedMd5: null,
            command.ContentId,
            command.ContentType,
            cancellationToken);

        logger.LogInformation("小文件上传：{FileName}（{Size} 字节），成功={Success}，文件ID={FileId}",
            command.FileName, command.Content.Length, result.Success, result.FileId);
        return result;
    }
}

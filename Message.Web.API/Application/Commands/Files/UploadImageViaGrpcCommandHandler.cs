
namespace Message.Web.API.Application.Commands.Files;
/// <summary>
/// 图片上传命令处理程序。
/// </summary>
public class UploadImageViaGrpcCommandHandler(
    IFileStorageGrpcClient fileStorage,
    ILogger<UploadImageViaGrpcCommandHandler> logger) : IRequestHandler<UploadImageViaGrpcCommand, UploadImageResult>
{
    public async Task<UploadImageResult> Handler(UploadImageViaGrpcCommand command, CancellationToken cancellationToken)
    {
        var result = await fileStorage.UploadImageAsync(
            command.UserId,
            command.FileName,
            command.Content,
            command.Description,
            command.ValidateFormat,
            cancellationToken);

        logger.LogInformation("图片上传：{FileName}（{Size} 字节），成功={Success}，文件ID={FileId}",
            command.FileName, command.Content.Length, result.Success, result.FileId);
        return result;
    }
}

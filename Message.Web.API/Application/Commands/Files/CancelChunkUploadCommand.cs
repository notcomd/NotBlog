namespace Message.Web.API.Application.Commands.Files;

/// <summary>
/// 取消分片上传命令。
/// </summary>
public record CancelChunkUploadCommand(string FileKey) : IRequest<CancelChunkUploadResult>;

namespace Message.Web.API.Application.Commands.Files;

/// <summary>
/// 通过 gRPC 上传文件命令。
/// </summary>
public record UploadFileViaGrpcCommand(
    Guid UserId,
    string FileName,
    byte[] Content,
    string? Description,
    bool IsPublic) : IRequest<UploadFileResult>;

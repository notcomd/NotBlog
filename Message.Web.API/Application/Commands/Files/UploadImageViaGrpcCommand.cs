namespace Message.Web.API.Application.Commands.Files;

/// <summary>
/// 通过 gRPC 上传图片命令。
/// </summary>
public record UploadImageViaGrpcCommand(
    Guid UserId,
    string FileName,
    byte[] Content,
    string? Description,
    bool ValidateFormat) : IRequest<UploadImageResult>;

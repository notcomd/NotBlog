namespace Message.Web.API.Dto.Response;
/// <summary>通用文件上传结果（调用 FileDev gRPC 服务后返回）</summary>
public record UploadFileResult(
    bool Success,
    Guid? FileId,
    Uri? FileUri,
    string FileMd5,
    long FileSize,
    string? ErrorMessage);


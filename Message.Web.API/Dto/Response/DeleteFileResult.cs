namespace Message.Web.API.Dto.Response;
/// <summary>删除文件结果（FileDev gRPC DeleteFile 的封装）</summary>
public record DeleteFileResult(bool Success, string? ErrorMessage);


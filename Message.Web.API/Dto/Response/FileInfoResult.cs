namespace Message.Web.API.Dto.Response;
/// <summary>文件信息查询结果（FileDev gRPC GetFileInfo 的封装）</summary>
public record FileInfoResult(
    bool Success,
    Guid? FileId,
    Guid? UserId,
    string FileName,
    long FileSize,
    Uri? FileUri,
    string FileMd5,
    string FileType,
    string? ErrorMessage);


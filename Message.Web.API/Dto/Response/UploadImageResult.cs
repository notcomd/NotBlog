namespace Message.Web.API.Dto.Response;
/// <summary>图片上传结果</summary>
public record UploadImageResult(
    bool Success,
    Guid? FileId,
    Uri? FileUri,
    string FileMd5,
    long FileSize,
    int Width,
    int Height,
    string Format,
    string? ErrorMessage);


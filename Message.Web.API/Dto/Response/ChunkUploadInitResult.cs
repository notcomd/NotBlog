namespace Message.Web.API.Dto.Response;
/// <summary>分片上传初始化结果</summary>
public record ChunkUploadInitResult(
    bool Success,
    string FileKey,
    int TotalChunks,
    int ChunkSize,
    IReadOnlyList<int> UploadedChunks,
    string? ErrorMessage);


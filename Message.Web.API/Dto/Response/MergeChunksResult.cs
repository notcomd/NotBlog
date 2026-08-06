namespace Message.Web.API.Dto.Response;
/// <summary>分片合并结果</summary>
public record MergeChunksResult(
    bool Success,
    Guid? FileId,
    Uri? FileUri,
    string FileMd5,
    long FileSize,
    string? ErrorMessage);


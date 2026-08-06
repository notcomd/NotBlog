namespace Message.Web.API.Dto.Response;
/// <summary>单个分片上传结果</summary>
public record ChunkUploadResult(
    bool Success,
    int ChunkIndex,
    string ChunkMd5,
    string? ErrorMessage);


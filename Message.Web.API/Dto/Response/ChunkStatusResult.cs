namespace Message.Web.API.Dto.Response;
/// <summary>分片上传状态查询结果（用于断点续传：跳过已上传分片）</summary>
public record ChunkStatusResult(
    bool Success,
    string FileKey,
    int TotalChunks,
    IReadOnlyList<int> UploadedChunks,
    string Status,
    string? ErrorMessage)
{
    /// <summary>是否所有分片均已上传完成</summary>
    public bool IsComplete => Success && TotalChunks > 0 && UploadedChunks.Count >= TotalChunks;

    /// <summary>已完成进度百分比（0-100）</summary>
    public double Percent => TotalChunks > 0 ? (double)UploadedChunks.Count / TotalChunks * 100 : 0;
}


namespace FileDev.Web.API.Application.Queries;

/// <summary>分片上传状态查询响应 DTO</summary>
public class ChunkStatusResponse
{
    public string FileKey { get; set; } = null!;
    public int TotalChunks { get; set; }
    public List<int> UploadedChunks { get; set; } = [];
    public bool IsComplete { get; set; }
    public ChunkUploadStatus Status { get; set; }
}

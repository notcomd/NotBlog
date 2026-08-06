namespace Message.Web.API.Dto.Request;
/// <summary>查询分片上传状态请求（用于断点续传）</summary>
public class ChunkStatusRequest
{
    /// <summary>分片上传记录键</summary>
    public string FileKey { get; init; } = string.Empty;
}


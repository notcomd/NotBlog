namespace Message.Web.API.Dto.Request;
/// <summary>取消分片上传请求</summary>
public class ChunkCancelRequest
{
    /// <summary>分片上传记录键</summary>
    public string FileKey { get; init; } = string.Empty;
}


namespace Message.Web.API.Dto.Request;
/// <summary>合并分片请求</summary>
public class ChunkMergeRequest
{
    /// <summary>分片上传记录键</summary>
    public string FileKey { get; init; } = string.Empty;

    /// <summary>最终文件名（可选，默认使用初始化时的文件名）</summary>
    public string? FileName { get; init; }

    /// <summary>文件描述</summary>
    public string? Description { get; init; }
}


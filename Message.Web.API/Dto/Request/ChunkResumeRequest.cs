namespace Message.Web.API.Dto.Request;
/// <summary>
/// 断点续传请求：一次性提交缺失分片集合，
/// 服务端查询已上传分片后仅上传缺失部分，并实时推送上传进度。
/// </summary>
public class ChunkResumeRequest
{
    /// <summary>分片上传记录键</summary>
    public string FileKey { get; init; } = string.Empty;

    /// <summary>总分片数</summary>
    public int TotalChunks { get; init; }

    /// <summary>单个分片大小（字节），用于与服务端分片参数核对</summary>
    public int ChunkSize { get; init; }

    /// <summary>待上传分片集合（分片索引 → 分片二进制数据）</summary>
    public Dictionary<int, byte[]> Chunks { get; init; } = [];
}


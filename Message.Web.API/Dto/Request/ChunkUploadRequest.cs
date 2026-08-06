namespace Message.Web.API.Dto.Request;
/// <summary>上传单个分片请求</summary>
public class ChunkUploadRequest
{
    /// <summary>分片上传记录键（由初始化接口返回）</summary>
    public string FileKey { get; init; } = string.Empty;

    /// <summary>分片索引（从0开始）</summary>
    public int ChunkIndex { get; init; }

    /// <summary>分片二进制数据</summary>
    public byte[] ChunkData { get; init; } = [];

    /// <summary>分片 MD5（可选，用于服务端一致性校验）</summary>
    public string? ChunkMd5 { get; init; }
}


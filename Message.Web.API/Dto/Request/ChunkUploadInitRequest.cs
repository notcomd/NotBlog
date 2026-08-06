namespace Message.Web.API.Dto.Request;
/// <summary>初始化分片上传请求</summary>
public class ChunkUploadInitRequest
{
    /// <summary>文件名（含扩展名）</summary>
    public string FileName { get; init; } = string.Empty;

    /// <summary>文件总大小（字节）</summary>
    public long TotalSize { get; init; }

    /// <summary>文件整体 MD5（可选，用于完整性校验与秒传）</summary>
    public string? FileMd5 { get; init; }

    /// <summary>是否公开文件（默认私有）</summary>
    public bool IsPublic { get; init; }

    /// <summary>文件描述</summary>
    public string? Description { get; init; }
}


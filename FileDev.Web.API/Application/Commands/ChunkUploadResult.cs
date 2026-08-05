namespace FileDev.Web.API.Application.Commands;

/// <summary>分片上传结果（返回实际哈希，供 gRPC 响应回显）</summary>
public record ChunkUploadResult(int ChunkIndex, string ChunkMd5);

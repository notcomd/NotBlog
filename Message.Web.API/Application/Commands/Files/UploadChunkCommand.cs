namespace Message.Web.API.Application.Commands.Files;

/// <summary>
/// 上传单个分片命令。
/// </summary>
public record UploadChunkCommand(
    string FileKey,
    int ChunkIndex,
    byte[] ChunkData,
    string? ChunkMd5) : IRequest<ChunkUploadResult>;

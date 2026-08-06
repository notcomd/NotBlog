namespace Message.Web.API.Application.Commands.Files;

/// <summary>
/// 断点续传命令。
/// </summary>
public record ResumeChunkUploadCommand(
    string FileKey,
    int TotalChunks,
    int ChunkSize,
    IReadOnlyDictionary<int, byte[]> Chunks) : IRequest<ChunkStatusResult>;

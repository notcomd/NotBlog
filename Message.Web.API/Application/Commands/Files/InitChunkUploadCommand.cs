namespace Message.Web.API.Application.Commands.Files;

/// <summary>
/// 初始化分片上传命令。
/// </summary>
public record InitChunkUploadCommand(
    Guid UserId,
    string FileName,
    long TotalSize,
    string? FileMd5,
    string? Description,
    bool IsPublic) : IRequest<ChunkUploadInitResult>;

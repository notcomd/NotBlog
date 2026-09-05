namespace Message.Web.API.Application.Commands.Files;

/// <summary>
/// 合并分片命令。
/// </summary>
public record MergeChunksCommand(
    string FileKey,
    Guid UserId,
    string? FileName,
    string? Description,
    Guid? ContentId = null,
    ContentReferenceType? ContentType = null) : IRequest<MergeChunksResult>;

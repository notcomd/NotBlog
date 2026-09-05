namespace FileDev.Web.API.Application.Commands;

/// <summary>将文件从标签中移除。文件本身保留，仅失去该标签归属。</summary>
public record TagRemoveFileCommand(
    Guid UserId,
    Guid TagId,
    Guid FileId) : IRequest<bool>;
namespace FileDev.Web.API.Application.Commands;

/// <summary>将文件加入标签（多对多，自动去重）。文件与标签均须属于当前用户。</summary>
public record TagAddFileCommand(
    Guid UserId,
    Guid TagId,
    Guid FileId) : IRequest<bool>;
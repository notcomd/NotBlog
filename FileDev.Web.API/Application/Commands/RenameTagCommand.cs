namespace FileDev.Web.API.Application.Commands;

/// <summary>重命名标签。默认标签（图片/文档/文件/视频）允许改名，改名的默认标签仍承担自动归类职责。</summary>
public record RenameTagCommand(
    Guid UserId,
    Guid TagId,
    string NewName) : IRequest<bool>;
namespace Message.Web.API.Application.Commands.Files;

/// <summary>
/// 删除文件命令。
/// </summary>
public record DeleteFileCommand(Guid FileId) : IRequest<bool>;

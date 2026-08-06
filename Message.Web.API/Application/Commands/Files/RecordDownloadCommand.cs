namespace Message.Web.API.Application.Commands.Files;

/// <summary>
/// 记录文件下载命令。
/// </summary>
public record RecordDownloadCommand(Guid FileId) : IRequest<bool>;

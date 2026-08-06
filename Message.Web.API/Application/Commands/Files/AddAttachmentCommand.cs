namespace Message.Web.API.Application.Commands.Files;

/// <summary>
/// 给消息添加附件命令（FileDev 文件 ID 校验归属）。
/// </summary>
public record AddAttachmentCommand(Guid CallerId, Guid MessageId, Guid FileId) : IRequest<Guid>;

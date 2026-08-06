
namespace Message.Web.API.Application.Commands.Messages;
/// <summary>
/// 标记消息为已读命令。
/// </summary>
/// <param name="MessageId">消息 ID</param>
/// <param name="UserId">阅读者用户 ID</param>
public record MarkMessageAsReadCommand(Guid MessageId, Guid UserId) : IRequest<bool>;


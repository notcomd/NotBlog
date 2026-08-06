
namespace Message.Web.API.Application.Commands.Messages;
/// <summary>
/// 撤回消息命令。
/// </summary>
/// <param name="MessageId">消息 ID</param>
/// <param name="UserId">撤回者用户 ID</param>
/// <param name="Reason">撤回原因</param>
public record RecallMessageCommand(Guid MessageId, Guid UserId, RecallReason Reason) : IRequest<bool>;


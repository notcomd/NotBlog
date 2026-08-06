using MessageEntity = Message.Domain.Entities.Message;

namespace Message.Web.API.Application.Commands.Messages;
/// <summary>
/// 转发消息命令。
/// <para>CQRS 命令侧：仅返回新消息的标识（Guid），不返回业务实体/DTO。</para>
/// </summary>
/// <param name="MessageId">源消息 ID</param>
/// <param name="TargetSessionId">目标会话 ID</param>
/// <param name="ForwardedBy">转发者用户 ID</param>
/// <param name="ForwardType">转发类型</param>
/// <param name="Comment">转发附言</param>
public record ForwardMessageCommand(
    Guid MessageId,
    Guid TargetSessionId,
    Guid ForwardedBy,
    ForwardType ForwardType,
    string? Comment) : IRequest<Guid>;


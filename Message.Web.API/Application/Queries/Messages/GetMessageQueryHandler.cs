using MessageEntity = Message.Domain.Entities.Chat.Message;

namespace Message.Web.API.Application.Queries.Messages;
/// <summary>
/// 获取消息详情查询处理程序。
/// <para>纯查询：不修改任何数据状态。</para>
/// <para>权限（修复 S-05）：仅消息所在会话的参与者可查看，非参与者返回 null。</para>
/// </summary>
public class GetMessageQueryHandler(
    IMessageRepository messageRepository,
    IChatSessionRepository sessionRepository,
    ICurrentUserService currentUser) : IRequestHandler<GetMessageQuery, MessageEntity?>
{
    public async Task<MessageEntity?> Handler(GetMessageQuery query, CancellationToken cancellationToken)
    {
        var message = await messageRepository.GetByIdAsync(query.MessageId);
        if (message == null)
            return null;

        var session = await sessionRepository.GetByIdAsync(message.SessionId);
        if (session == null)
            return null;

        var callerId = currentUser.GetUserId();
        return session.IsParticipant(callerId) ? message : null;
    }
}

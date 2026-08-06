using MessageEntity = Message.Domain.Entities.Message;

namespace Message.Web.API.Application.Queries.Messages;
/// <summary>
/// 获取会话消息列表查询处理程序。
/// <para>纯查询：不修改任何数据状态。</para>
/// <para>权限（修复 S-05）：仅会话参与者可查看消息列表，非参与者返回空列表（不泄露会话存在性）。</para>
/// </summary>
public class GetSessionMessagesQueryHandler(
    IMessageRepository messageRepository,
    IChatSessionRepository sessionRepository,
    ICurrentUserService currentUser) : IRequestHandler<GetSessionMessagesQuery, PagedResult<MessageEntity>>
{
    public async Task<PagedResult<MessageEntity>> Handler(GetSessionMessagesQuery query,
        CancellationToken cancellationToken)
    {
        var callerId = currentUser.GetUserId();
        var session = await sessionRepository.GetByIdAsync(query.SessionId);
        if (session == null || !session.IsParticipant(callerId))
        {
            return new PagedResult<MessageEntity>
            {
                Items = [],
                TotalCount = 0,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }

        var messages = await messageRepository.GetBySessionIdAsync(query.SessionId, query.Page, query.PageSize);
        var totalCount = await messageRepository.GetMessageCountBySessionAsync(query.SessionId);

        return new PagedResult<MessageEntity>
        {
            Items = messages.ToList(),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }
}

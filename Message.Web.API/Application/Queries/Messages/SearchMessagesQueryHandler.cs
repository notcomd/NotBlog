using MessageEntity = Message.Domain.Entities.Chat.Message;

namespace Message.Web.API.Application.Queries.Messages;
/// <summary>
/// 在会话内搜索消息查询处理程序。
/// <para>纯查询：不修改任何数据状态。</para>
/// <para>权限（修复 S-05）：仅会话参与者可搜索会话消息，非参与者返回空列表。</para>
/// </summary>
public class SearchMessagesQueryHandler(
    IMessageRepository messageRepository,
    IChatSessionRepository sessionRepository,
    ICurrentUserService currentUser) : IRequestHandler<SearchMessagesQuery, PagedResult<MessageEntity>>
{
    public async Task<PagedResult<MessageEntity>> Handler(SearchMessagesQuery query,
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

        var messages = await messageRepository.SearchAsync(
            query.SessionId, query.SearchTerm, query.Page, query.PageSize);
        var totalCount = await messageRepository.SearchCountAsync(query.SessionId, query.SearchTerm);

        return new PagedResult<MessageEntity>
        {
            Items = messages.ToList(),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }
}

using MessageEntity = Message.Domain.Entities.Message;

namespace Message.Web.API.Application.Queries.Messages;

/// <summary>
/// 获取会话消息列表查询（分页）。
/// </summary>
/// <param name="SessionId">会话 ID</param>
/// <param name="Page">页码（从1开始）</param>
/// <param name="PageSize">每页条数</param>
public record GetSessionMessagesQuery(Guid SessionId, int Page, int PageSize)
    : IRequest<PagedResult<MessageEntity>>;

/// <summary>
/// 获取会话消息列表查询处理程序。
/// <para>纯查询：不修改任何数据状态。</para>
/// </summary>
public class GetSessionMessagesQueryHandler(
    IMessageRepository messageRepository) : IRequestHandler<GetSessionMessagesQuery, PagedResult<MessageEntity>>
{
    public async Task<PagedResult<MessageEntity>> Handler(GetSessionMessagesQuery query,
        CancellationToken cancellationToken)
    {
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

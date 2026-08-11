using MessageEntity = Message.Domain.Entities.Chat.Message;

namespace Message.Web.API.Application.Queries.Messages;
/// <summary>
/// 获取会话消息列表查询（分页）。
/// </summary>
/// <param name="SessionId">会话 ID</param>
/// <param name="Page">页码（从1开始）</param>
/// <param name="PageSize">每页条数</param>
public record GetSessionMessagesQuery(Guid SessionId, int Page, int PageSize)
    : IRequest<PagedResult<MessageEntity>>;


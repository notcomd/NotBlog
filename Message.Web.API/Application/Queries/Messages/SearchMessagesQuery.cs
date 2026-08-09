using MessageEntity = Message.Domain.Entities.Chat.Message;

namespace Message.Web.API.Application.Queries.Messages;
/// <summary>
/// 在会话内搜索消息查询（分页）。
/// </summary>
/// <param name="SessionId">会话 ID</param>
/// <param name="SearchTerm">搜索关键词</param>
/// <param name="Page">页码（从1开始）</param>
/// <param name="PageSize">每页条数</param>
public record SearchMessagesQuery(Guid SessionId, string SearchTerm, int Page, int PageSize)
    : IRequest<PagedResult<MessageEntity>>;


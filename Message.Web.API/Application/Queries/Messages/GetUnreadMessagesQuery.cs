using MessageEntity = Message.Domain.Entities.Message;

namespace Message.Web.API.Application.Queries.Messages;
/// <summary>
/// 获取当前用户未读消息列表查询。
/// </summary>
/// <param name="UserId">用户 ID</param>
public record GetUnreadMessagesQuery(Guid UserId) : IRequest<IEnumerable<MessageEntity>>;


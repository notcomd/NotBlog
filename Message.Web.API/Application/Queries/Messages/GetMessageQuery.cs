using MessageEntity = Message.Domain.Entities.Message;

namespace Message.Web.API.Application.Queries.Messages;
/// <summary>
/// 获取消息详情查询。
/// </summary>
/// <param name="MessageId">消息 ID</param>
public record GetMessageQuery(Guid MessageId) : IRequest<MessageEntity?>;


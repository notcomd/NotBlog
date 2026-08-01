using MessageEntity = Message.Domain.Entities.Message;

namespace Message.Web.API.Application.Queries.Messages;

/// <summary>
/// 获取消息详情查询。
/// </summary>
/// <param name="MessageId">消息 ID</param>
public record GetMessageQuery(Guid MessageId) : IRequest<MessageEntity?>;

/// <summary>
/// 获取消息详情查询处理程序。
/// <para>纯查询：不修改任何数据状态。</para>
/// </summary>
public class GetMessageQueryHandler(
    IMessageRepository messageRepository) : IRequestHandler<GetMessageQuery, MessageEntity?>
{
    public async Task<MessageEntity?> Handler(GetMessageQuery query, CancellationToken cancellationToken)
        => await messageRepository.GetByIdAsync(query.MessageId);
}

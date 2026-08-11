using MessageEntity = Message.Domain.Entities.Chat.Message;

namespace Message.Web.API.Application.Queries.Messages;
/// <summary>
/// 获取当前用户未读消息列表查询处理程序。
/// <para>纯查询：不修改任何数据状态。</para>
/// </summary>
public class GetUnreadMessagesQueryHandler(
    IMessageRepository messageRepository) : IRequestHandler<GetUnreadMessagesQuery, IEnumerable<MessageEntity>>
{
    public async Task<IEnumerable<MessageEntity>> Handler(GetUnreadMessagesQuery query,
        CancellationToken cancellationToken)
        => await messageRepository.GetUnreadMessagesAsync(query.UserId);
}

namespace Message.Web.API.Application.Queries.Friends;
/// <summary>
/// 获取发出的好友请求列表查询处理程序。
/// <para>纯查询：不修改任何数据状态。</para>
/// </summary>
public class GetSentRequestsQueryHandler(
    IMessageFriendsRepository friendRepository) : IRequestHandler<GetSentRequestsQuery, IEnumerable<MessageFriends>>
{
    public async Task<IEnumerable<MessageFriends>> Handler(GetSentRequestsQuery query, CancellationToken cancellationToken)
        => await friendRepository.GetSentRequestsAsync(query.UserId);
}

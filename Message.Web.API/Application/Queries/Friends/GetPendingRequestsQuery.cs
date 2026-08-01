namespace Message.Web.API.Application.Queries.Friends;

/// <summary>
/// 获取收到的好友请求列表查询。
/// </summary>
/// <param name="UserId">用户 ID</param>
public record GetPendingRequestsQuery(Guid UserId) : IRequest<IEnumerable<MessageFriends>>;

/// <summary>
/// 获取收到的好友请求列表查询处理程序。
/// <para>纯查询：不修改任何数据状态。</para>
/// </summary>
public class GetPendingRequestsQueryHandler(
    IMessageFriendsRepository friendRepository) : IRequestHandler<GetPendingRequestsQuery, IEnumerable<MessageFriends>>
{
    public async Task<IEnumerable<MessageFriends>> Handler(GetPendingRequestsQuery query, CancellationToken cancellationToken)
        => await friendRepository.GetPendingRequestsAsync(query.UserId);
}

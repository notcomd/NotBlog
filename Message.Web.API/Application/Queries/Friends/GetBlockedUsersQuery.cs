namespace Message.Web.API.Application.Queries.Friends;

/// <summary>
/// 获取已屏蔽的好友列表查询。
/// </summary>
/// <param name="UserId">用户 ID</param>
public record GetBlockedUsersQuery(Guid UserId) : IRequest<IEnumerable<MessageFriends>>;

/// <summary>
/// 获取已屏蔽的好友列表查询处理程序。
/// <para>纯查询：不修改任何数据状态。</para>
/// </summary>
public class GetBlockedUsersQueryHandler(
    IMessageFriendsRepository friendRepository) : IRequestHandler<GetBlockedUsersQuery, IEnumerable<MessageFriends>>
{
    public async Task<IEnumerable<MessageFriends>> Handler(GetBlockedUsersQuery query, CancellationToken cancellationToken)
        => await friendRepository.GetBlockedUsersAsync(query.UserId);
}

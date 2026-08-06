namespace Message.Web.API.Application.Queries.Friends;
/// <summary>
/// 获取星标好友列表查询处理程序。
/// <para>纯查询：不修改任何数据状态。</para>
/// </summary>
public class GetStarredFriendsQueryHandler(
    IMessageFriendsRepository friendRepository) : IRequestHandler<GetStarredFriendsQuery, IEnumerable<MessageFriends>>
{
    public async Task<IEnumerable<MessageFriends>> Handler(GetStarredFriendsQuery query, CancellationToken cancellationToken)
        => await friendRepository.GetStarredFriendsAsync(query.UserId);
}

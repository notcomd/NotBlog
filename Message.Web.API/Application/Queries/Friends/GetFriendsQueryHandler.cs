namespace Message.Web.API.Application.Queries.Friends;
/// <summary>
/// 获取好友列表查询处理程序。
/// <para>纯查询：不修改任何数据状态。</para>
/// </summary>
public class GetFriendsQueryHandler(
    IMessageFriendsRepository friendRepository) : IRequestHandler<GetFriendsQuery, IEnumerable<MessageFriends>>
{
    public async Task<IEnumerable<MessageFriends>> Handler(GetFriendsQuery query, CancellationToken cancellationToken)
        => await friendRepository.GetAcceptedFriendsAsync(query.UserId);
}

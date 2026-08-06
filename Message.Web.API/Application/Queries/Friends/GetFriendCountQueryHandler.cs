namespace Message.Web.API.Application.Queries.Friends;
/// <summary>
/// 获取好友数量查询处理程序。
/// <para>纯查询：不修改任何数据状态。</para>
/// </summary>
public class GetFriendCountQueryHandler(
    IMessageFriendsRepository friendRepository) : IRequestHandler<GetFriendCountQuery, int>
{
    public async Task<int> Handler(GetFriendCountQuery query, CancellationToken cancellationToken)
        => await friendRepository.GetFriendCountAsync(query.UserId);
}

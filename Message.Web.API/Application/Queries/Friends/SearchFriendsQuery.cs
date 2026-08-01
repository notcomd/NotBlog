namespace Message.Web.API.Application.Queries.Friends;

/// <summary>
/// 搜索好友查询。
/// </summary>
/// <param name="UserId">用户 ID</param>
/// <param name="SearchTerm">搜索关键词</param>
public record SearchFriendsQuery(Guid UserId, string SearchTerm) : IRequest<IEnumerable<MessageFriends>>;

/// <summary>
/// 搜索好友查询处理程序。
/// <para>纯查询：不修改任何数据状态。</para>
/// </summary>
public class SearchFriendsQueryHandler(
    IMessageFriendsRepository friendRepository) : IRequestHandler<SearchFriendsQuery, IEnumerable<MessageFriends>>
{
    public async Task<IEnumerable<MessageFriends>> Handler(SearchFriendsQuery query, CancellationToken cancellationToken)
        => await friendRepository.SearchFriendsAsync(query.UserId, query.SearchTerm);
}

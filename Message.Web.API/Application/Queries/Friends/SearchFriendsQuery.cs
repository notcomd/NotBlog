namespace Message.Web.API.Application.Queries.Friends;
/// <summary>
/// 搜索好友查询。
/// </summary>
/// <param name="UserId">用户 ID</param>
/// <param name="SearchTerm">搜索关键词</param>
public record SearchFriendsQuery(Guid UserId, string SearchTerm) : IRequest<IEnumerable<MessageFriends>>;


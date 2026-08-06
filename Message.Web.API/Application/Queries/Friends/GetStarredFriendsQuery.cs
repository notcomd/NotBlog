namespace Message.Web.API.Application.Queries.Friends;
/// <summary>
/// 获取星标好友列表查询。
/// </summary>
/// <param name="UserId">用户 ID</param>
public record GetStarredFriendsQuery(Guid UserId) : IRequest<IEnumerable<MessageFriends>>;


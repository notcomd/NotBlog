namespace Message.Web.API.Application.Queries.Friends;
/// <summary>
/// 获取已屏蔽的好友列表查询。
/// </summary>
/// <param name="UserId">用户 ID</param>
public record GetBlockedUsersQuery(Guid UserId) : IRequest<IEnumerable<MessageFriends>>;


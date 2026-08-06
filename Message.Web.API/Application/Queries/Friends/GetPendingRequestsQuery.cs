namespace Message.Web.API.Application.Queries.Friends;
/// <summary>
/// 获取收到的好友请求列表查询。
/// </summary>
/// <param name="UserId">用户 ID</param>
public record GetPendingRequestsQuery(Guid UserId) : IRequest<IEnumerable<MessageFriends>>;


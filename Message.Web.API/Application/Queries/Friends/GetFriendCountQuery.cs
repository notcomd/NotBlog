namespace Message.Web.API.Application.Queries.Friends;
/// <summary>
/// 获取好友数量查询。
/// </summary>
/// <param name="UserId">用户 ID</param>
public record GetFriendCountQuery(Guid UserId) : IRequest<int>;


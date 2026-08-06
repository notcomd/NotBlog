namespace Message.Web.API.Application.Queries.Groups;
/// <summary>
/// 获取用户加入的群组列表查询。
/// </summary>
/// <param name="UserId">用户 ID</param>
public record GetUserGroupsQuery(Guid UserId) : IRequest<IEnumerable<Group>>;


namespace Message.Web.API.Application.Queries.Groups;
/// <summary>
/// 获取群组成员列表查询。
/// </summary>
/// <param name="GroupId">群组 ID</param>
public record GetGroupMembersQuery(Guid GroupId) : IRequest<IEnumerable<GroupMember>>;


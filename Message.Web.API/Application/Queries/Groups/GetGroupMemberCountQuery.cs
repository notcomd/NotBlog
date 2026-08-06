namespace Message.Web.API.Application.Queries.Groups;
/// <summary>
/// 获取群组成员数量查询。
/// </summary>
/// <param name="GroupId">群组 ID</param>
public record GetGroupMemberCountQuery(Guid GroupId) : IRequest<int>;


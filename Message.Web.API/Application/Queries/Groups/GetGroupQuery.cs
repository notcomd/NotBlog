namespace Message.Web.API.Application.Queries.Groups;
/// <summary>
/// 获取群组详情查询。
/// </summary>
/// <param name="GroupId">群组 ID</param>
public record GetGroupQuery(Guid GroupId) : IRequest<Group?>;


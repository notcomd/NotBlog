namespace Message.Web.API.Application.Queries.Groups;
/// <summary>
/// 获取公开群组列表查询。
/// </summary>
public record GetPublicGroupsQuery() : IRequest<IEnumerable<Group>>;


namespace Message.Web.API.Application.Queries.Groups;

/// <summary>
/// 获取公开群组列表查询。
/// </summary>
public record GetPublicGroupsQuery() : IRequest<IEnumerable<Group>>;

/// <summary>
/// 获取公开群组列表查询处理程序。
/// <para>纯查询：不修改任何数据状态。</para>
/// </summary>
public class GetPublicGroupsQueryHandler(
    IGroupRepository groupRepository) : IRequestHandler<GetPublicGroupsQuery, IEnumerable<Group>>
{
    public async Task<IEnumerable<Group>> Handler(GetPublicGroupsQuery query, CancellationToken cancellationToken)
        => await groupRepository.GetPublicGroupsAsync();
}

namespace Message.Web.API.Application.Queries.Groups;
/// <summary>
/// 搜索群组查询处理程序。
/// <para>纯查询：不修改任何数据状态。</para>
/// </summary>
public class SearchGroupsQueryHandler(
    IGroupRepository groupRepository) : IRequestHandler<SearchGroupsQuery, IEnumerable<Group>>
{
    public async Task<IEnumerable<Group>> Handler(SearchGroupsQuery query, CancellationToken cancellationToken)
        => await groupRepository.SearchAsync(query.SearchTerm, query.Page, query.PageSize);
}

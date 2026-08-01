namespace Message.Web.API.Application.Queries.Groups;

/// <summary>
/// 搜索群组查询（分页）。
/// </summary>
/// <param name="SearchTerm">搜索关键词</param>
/// <param name="Page">页码（从1开始）</param>
/// <param name="PageSize">每页条数</param>
public record SearchGroupsQuery(string SearchTerm, int Page, int PageSize) : IRequest<IEnumerable<Group>>;

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

namespace Message.Web.API.Application.Queries.Community;

/// <summary>圈子发现列表查询处理程序。</summary>
public class GetCirclesQueryHandler(
    ICircleRepository circleRepository) : IRequestHandler<GetCirclesQuery, PagedResult<Circle>>
{
    public async Task<PagedResult<Circle>> Handler(GetCirclesQuery query, CancellationToken cancellationToken)
    {
        var items = await circleRepository.GetActiveAsync(query.Keyword, query.Page, query.PageSize);
        var total = await circleRepository.GetActiveCountAsync(query.Keyword);

        return new PagedResult<Circle>
        {
            Items = items.ToList(),
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }
}
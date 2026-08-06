namespace Message.Web.API.Application.Queries.Community;

/// <summary>话题列表查询处理程序。</summary>
public class GetTopicsQueryHandler(
    ITopicRepository topicRepository) : IRequestHandler<GetTopicsQuery, PagedResult<Topic>>
{
    public async Task<PagedResult<Topic>> Handler(GetTopicsQuery query, CancellationToken cancellationToken)
    {
        var items = await topicRepository.GetActiveAsync(query.Page, query.PageSize);
        var total = await topicRepository.GetActiveCountAsync();

        return new PagedResult<Topic>
        {
            Items = items.ToList(),
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }
}

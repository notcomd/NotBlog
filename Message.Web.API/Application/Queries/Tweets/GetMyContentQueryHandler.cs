namespace Message.Web.API.Application.Queries.Tweets;

/// <summary>
/// 我的内容列表查询处理程序（按作者 + 可选状态过滤，按创建时间倒序）。
/// <para>纯查询：不修改任何数据状态。作者查询自己的内容，不受可见性过滤（可见全部状态）。</para>
/// </summary>
public class GetMyContentQueryHandler(
    ITweetRepository tweetRepository) : IRequestHandler<GetMyContentQuery, PagedResult<Tweet>>
{
    public async Task<PagedResult<Tweet>> Handler(GetMyContentQuery query, CancellationToken cancellationToken)
    {
        var items = await tweetRepository.GetByAuthorWithStatusAsync(query.UserId, query.Status, query.Page, query.PageSize);
        var totalCount = await tweetRepository.CountByAuthorWithStatusAsync(query.UserId, query.Status);

        return new PagedResult<Tweet>
        {
            Items = items.ToList(),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }
}

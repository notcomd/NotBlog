namespace Message.Web.API.Application.Queries.Tweets;

/// <summary>我的草稿列表查询（R-08，仅作者本人可见，按最近编辑倒序）。</summary>
public record GetMyDraftsQuery(Guid UserId, int Page, int PageSize) : IRequest<PagedResult<Tweet>>;

/// <summary>我的草稿列表查询处理程序。</summary>
public class GetMyDraftsQueryHandler(
    ITweetRepository tweetRepository) : IRequestHandler<GetMyDraftsQuery, PagedResult<Tweet>>
{
    public async Task<PagedResult<Tweet>> Handler(GetMyDraftsQuery query, CancellationToken cancellationToken)
    {
        var items = await tweetRepository.GetByAuthorAndStatusAsync(query.UserId, TweetStatus.Draft, query.Page, query.PageSize);
        var totalCount = await tweetRepository.CountByAuthorAndStatusAsync(query.UserId, TweetStatus.Draft);

        return new PagedResult<Tweet>
        {
            Items = items.ToList(),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }
}

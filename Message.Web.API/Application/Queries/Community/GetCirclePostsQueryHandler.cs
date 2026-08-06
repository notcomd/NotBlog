namespace Message.Web.API.Application.Queries.Community;

/// <summary>圈子帖子流查询处理程序。</summary>
public class GetCirclePostsQueryHandler(
    ICircleRepository circleRepository,
    ITweetRepository tweetRepository) : IRequestHandler<GetCirclePostsQuery, PagedResult<Tweet>>
{
    public async Task<PagedResult<Tweet>> Handler(GetCirclePostsQuery query, CancellationToken cancellationToken)
    {
        await CommunityAccessGuard.EnsureCircleMemberAsync(circleRepository, query.CircleGuid, query.CurrentUserId);

        var items = await tweetRepository.GetByCircleAsync(query.CircleGuid, query.Page, query.PageSize);
        var total = await tweetRepository.GetCirclePostCountAsync(query.CircleGuid);

        return new PagedResult<Tweet>
        {
            Items = items.ToList(),
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }
}

namespace Message.Web.API.Application.Queries.Tweets;

/// <summary>
/// 获取用户推文列表查询。
/// </summary>
/// <param name="UserGuid">用户 ID</param>
/// <param name="Page">页码（从1开始）</param>
/// <param name="PageSize">每页条数</param>
public record GetUserTweetsQuery(Guid UserGuid, int Page, int PageSize) : IRequest<PagedResult<Tweet>>;

/// <summary>
/// 获取用户推文列表查询处理程序。
/// <para>纯查询：不修改任何数据状态。</para>
/// </summary>
public class GetUserTweetsQueryHandler(
    ITweetRepository tweetRepository,
    ICurrentUserService currentUserService) : IRequestHandler<GetUserTweetsQuery, PagedResult<Tweet>>
{
    public async Task<PagedResult<Tweet>> Handler(GetUserTweetsQuery query, CancellationToken cancellationToken)
    {
        var allTweets = await tweetRepository.GetByAuthorAsync(query.UserGuid, query.Page, query.PageSize);
        var totalCount = await tweetRepository.GetCountByAuthorAsync(query.UserGuid);

        var currentUserId = currentUserService.IsAuthenticated ? currentUserService.GetUserId() : Guid.Empty;

        // S-17：可见性过滤 —— 作者本人可见全部；他人仅可见 Public（Private 仅作者；Followers 无关注关系退化为仅作者）
        var items = allTweets.Where(t =>
            TweetVisibilityPolicy.IsVisibleTo(t, currentUserId) &&
            (t.TweetStatus == TweetStatus.Approved ||
             (currentUserId != Guid.Empty && t.AuthorGuid == currentUserId && t.TweetStatus == TweetStatus.Draft)));

        return new PagedResult<Tweet>
        {
            Items = items.ToList(),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }
}
namespace Message.Web.API.Application.Queries.Tweets;

/// <summary>
/// 获取用户推文列表查询。
/// </summary>
/// <param name="UserGuid">用户 ID</param>
/// <param name="Page">页码（从1开始）</param>
/// <param name="PageSize">每页条数</param>
public record GetUserTweetsQuery(Guid UserGuid, int Page, int PageSize) : IRequest<IEnumerable<Tweet>>;

/// <summary>
/// 获取用户推文列表查询处理程序。
/// <para>纯查询：不修改任何数据状态。</para>
/// </summary>
public class GetUserTweetsQueryHandler(
    ITweetRepository tweetRepository,
    ICurrentUserService currentUserService) : IRequestHandler<GetUserTweetsQuery, IEnumerable<Tweet>>
{
    public async Task<IEnumerable<Tweet>> Handler(GetUserTweetsQuery query, CancellationToken cancellationToken)
    {
        var allTweets = await tweetRepository.GetByAuthorAsync(query.UserGuid, query.Page, query.PageSize);

        var currentUserId = currentUserService.IsAuthenticated ? currentUserService.GetUserId() : Guid.Empty;

        return allTweets.Where(t =>
            t.TweetStatus == TweetStatus.Approved ||
            (currentUserId != Guid.Empty && t.AuthorGuid == currentUserId && t.TweetStatus == TweetStatus.Draft));
    }
}

namespace Message.Web.API.Application.Queries.Tweets;
/// <summary>
/// 获取用户推文列表查询处理程序。
/// <para>纯查询：不修改任何数据状态。</para>
/// </summary>
public class GetUserTweetsQueryHandler(
    ITweetRepository tweetRepository,
    ICircleRepository circleRepository,
    ICurrentUserService currentUserService) : IRequestHandler<GetUserTweetsQuery, PagedResult<Tweet>>
{
    public async Task<PagedResult<Tweet>> Handler(GetUserTweetsQuery query, CancellationToken cancellationToken)
    {
        var allTweets = await tweetRepository.GetByAuthorAsync(query.UserGuid, query.Page, query.PageSize);
        var totalCount = await tweetRepository.GetCountByAuthorAsync(query.UserGuid);

        var currentUserId = currentUserService.IsAuthenticated ? currentUserService.GetUserId() : Guid.Empty;

        // S-17：可见性过滤 —— 作者本人可见全部；他人仅可见 Public（Private 仅作者；Followers 无关注关系退化为仅作者）
        // 圈子帖：查看者非圈子成员时隐藏（作者本人查看自己的帖子始终可见）
        var items = new List<Tweet>();
        foreach (var t in allTweets)
        {
            if (!TweetVisibilityPolicy.IsVisibleTo(t, currentUserId))
                continue;

            if (t.TweetStatus != TweetStatus.Approved
                && !(currentUserId != Guid.Empty && t.AuthorGuid == currentUserId && t.TweetStatus == TweetStatus.Draft))
                continue;

            if (t.CircleGuid is not null && t.AuthorGuid != currentUserId
                && (currentUserId == Guid.Empty || !await circleRepository.IsMemberAsync(t.CircleGuid.Value, currentUserId)))
                continue;

            items.Add(t);
        }

        return new PagedResult<Tweet>
        {
            Items = items,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }
}
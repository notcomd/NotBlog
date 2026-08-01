namespace Message.Web.API.Application.Queries.Tweets;

/// <summary>
/// 获取用户时间线推文列表查询。
/// </summary>
/// <param name="UserId">用户 ID</param>
/// <param name="Page">页码（从1开始）</param>
/// <param name="PageSize">每页条数</param>
public record GetTimelineQuery(Guid UserId, int Page, int PageSize) : IRequest<IEnumerable<Tweet>>;

/// <summary>
/// 获取用户时间线推文列表查询处理程序。
/// <para>纯查询：不修改任何数据状态。</para>
/// </summary>
public class GetTimelineQueryHandler(
    ITweetRepository tweetRepository) : IRequestHandler<GetTimelineQuery, IEnumerable<Tweet>>
{
    public async Task<IEnumerable<Tweet>> Handler(GetTimelineQuery query, CancellationToken cancellationToken)
        => await tweetRepository.GetTimelineAsync(new[] { query.UserId }, query.Page, query.PageSize);
}

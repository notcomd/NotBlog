namespace Message.Web.API.Application.Queries.Audit;

/// <summary>
/// 获取待审核推文列表查询。
/// </summary>
/// <param name="Page">页码（从1开始）</param>
/// <param name="PageSize">每页条数</param>
public record GetPendingTweetsQuery(int Page, int PageSize) : IRequest<IEnumerable<Tweet>>;

/// <summary>
/// 获取待审核推文列表查询处理程序。
/// <para>纯查询：不修改任何数据状态。</para>
/// </summary>
public class GetPendingTweetsQueryHandler(
    ITweetRepository tweetRepository) : IRequestHandler<GetPendingTweetsQuery, IEnumerable<Tweet>>
{
    public async Task<IEnumerable<Tweet>> Handler(GetPendingTweetsQuery query, CancellationToken cancellationToken)
        => await tweetRepository.GetByStatusAsync(TweetStatus.Pending, query.Page, query.PageSize);
}

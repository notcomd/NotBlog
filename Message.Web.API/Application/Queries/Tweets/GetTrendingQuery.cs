namespace Message.Web.API.Application.Queries.Tweets;

/// <summary>
/// 获取趋势推文列表查询。
/// </summary>
/// <param name="Page">页码（从1开始）</param>
/// <param name="PageSize">每页条数</param>
public record GetTrendingQuery(int Page, int PageSize) : IRequest<IEnumerable<Tweet>>;

/// <summary>
/// 获取趋势推文列表查询处理程序。
/// <para>纯查询：不修改任何数据状态。</para>
/// </summary>
public class GetTrendingQueryHandler(
    ITweetRepository tweetRepository) : IRequestHandler<GetTrendingQuery, IEnumerable<Tweet>>
{
    public async Task<IEnumerable<Tweet>> Handler(GetTrendingQuery query, CancellationToken cancellationToken)
        => await tweetRepository.GetTrendingAsync(query.Page, query.PageSize);
}

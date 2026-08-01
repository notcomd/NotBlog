namespace Message.Web.API.Application.Queries.Comments;

/// <summary>
/// 获取推文评论列表查询（分页）。
/// </summary>
/// <param name="TweetGuid">推文 ID</param>
/// <param name="Page">页码（从1开始）</param>
/// <param name="PageSize">每页条数</param>
public record GetTweetCommentsQuery(Guid TweetGuid, int Page, int PageSize) : IRequest<IEnumerable<Comment>>;

/// <summary>
/// 获取推文评论列表查询处理程序。
/// <para>纯查询：不修改任何数据状态。</para>
/// </summary>
public class GetTweetCommentsQueryHandler(
    ICommentRepository commentRepository) : IRequestHandler<GetTweetCommentsQuery, IEnumerable<Comment>>
{
    public async Task<IEnumerable<Comment>> Handler(GetTweetCommentsQuery query, CancellationToken cancellationToken)
        => await commentRepository.GetByTweetAsync(query.TweetGuid, query.Page, query.PageSize);
}

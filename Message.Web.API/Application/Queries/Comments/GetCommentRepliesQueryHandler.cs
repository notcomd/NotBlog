namespace Message.Web.API.Application.Queries.Comments;
/// <summary>
/// 获取评论回复列表查询处理程序。
/// <para>纯查询：不修改任何数据状态。</para>
/// </summary>
public class GetCommentRepliesQueryHandler(
    ICommentRepository commentRepository) : IRequestHandler<GetCommentRepliesQuery, IEnumerable<Comment>>
{
    public async Task<IEnumerable<Comment>> Handler(GetCommentRepliesQuery query, CancellationToken cancellationToken)
        => await commentRepository.GetRepliesAsync(query.CommentGuid, query.Page, query.PageSize);
}

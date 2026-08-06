namespace Message.Web.API.Application.Queries.Comments;
/// <summary>
/// 获取评论回复列表查询（分页）。
/// </summary>
/// <param name="CommentGuid">评论 ID</param>
/// <param name="Page">页码（从1开始）</param>
/// <param name="PageSize">每页条数</param>
public record GetCommentRepliesQuery(Guid CommentGuid, int Page, int PageSize) : IRequest<IEnumerable<Comment>>;


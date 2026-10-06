namespace Message.Web.API.Application.Queries.Tweets;

/// <summary>
/// 我的内容列表查询（当前登录用户自己的推文，可按状态过滤；按创建时间倒序）。
/// </summary>
/// <param name="UserId">当前登录用户 ID</param>
/// <param name="Status">推文状态过滤（null 表示全部状态）</param>
/// <param name="Page">页码（从1开始）</param>
/// <param name="PageSize">每页条数</param>
public record GetMyContentQuery(Guid UserId, TweetStatus? Status, int Page, int PageSize) : IRequest<PagedResult<Tweet>>;

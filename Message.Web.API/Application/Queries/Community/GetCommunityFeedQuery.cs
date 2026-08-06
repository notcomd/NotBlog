namespace Message.Web.API.Application.Queries.Community;

/// <summary>
/// 社区关注 Feed 查询：我 + 我关注的人发布的全局帖（仅 Approved，按时间倒序）。
/// <para>圈子帖不进入 Feed（圈子内容通过圈子频道消费）。</para>
/// </summary>
public record GetCommunityFeedQuery(Guid UserId, int Page, int PageSize) : IRequest<PagedResult<Tweet>>;


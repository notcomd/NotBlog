namespace Message.Web.API.Application.Queries.Sessions;
/// <summary>
/// 获取用户置顶会话列表查询。
/// </summary>
/// <param name="UserId">用户 ID</param>
public record GetPinnedSessionsQuery(Guid UserId) : IRequest<IEnumerable<ChatSession>>;


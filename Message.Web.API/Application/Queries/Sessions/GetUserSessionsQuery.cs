namespace Message.Web.API.Application.Queries.Sessions;
/// <summary>
/// 获取用户会话列表查询。
/// </summary>
/// <param name="UserId">用户 ID</param>
public record GetUserSessionsQuery(Guid UserId) : IRequest<IEnumerable<ChatSession>>;


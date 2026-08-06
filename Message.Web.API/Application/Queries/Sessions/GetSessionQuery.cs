namespace Message.Web.API.Application.Queries.Sessions;
/// <summary>
/// 获取会话详情查询。
/// </summary>
/// <param name="SessionId">会话 ID</param>
public record GetSessionQuery(Guid SessionId) : IRequest<ChatSession?>;


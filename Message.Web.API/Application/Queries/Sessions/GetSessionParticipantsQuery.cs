namespace Message.Web.API.Application.Queries.Sessions;
/// <summary>
/// 获取会话参与者列表查询。
/// <para>返回 null 表示会话不存在（由上层转换为 404）。</para>
/// </summary>
/// <param name="SessionId">会话 ID</param>
public record GetSessionParticipantsQuery(Guid SessionId) : IRequest<ChatSession?>;


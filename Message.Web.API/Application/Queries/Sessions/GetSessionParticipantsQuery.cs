namespace Message.Web.API.Application.Queries.Sessions;

/// <summary>
/// 获取会话参与者列表查询。
/// <para>返回 null 表示会话不存在（由上层转换为 404）。</para>
/// </summary>
/// <param name="SessionId">会话 ID</param>
public record GetSessionParticipantsQuery(Guid SessionId) : IRequest<ChatSession?>;

/// <summary>
/// 获取会话参与者列表查询处理程序。
/// <para>纯查询：不修改任何数据状态。</para>
/// <para>权限（修复 S-05）：仅会话参与者可查看参与者列表，非参与者返回 null。</para>
/// </summary>
public class GetSessionParticipantsQueryHandler(
    IChatSessionRepository sessionRepository,
    ICurrentUserService currentUser) : IRequestHandler<GetSessionParticipantsQuery, ChatSession?>
{
    public async Task<ChatSession?> Handler(GetSessionParticipantsQuery query, CancellationToken cancellationToken)
    {
        var session = await sessionRepository.GetByIdAsync(query.SessionId);
        if (session == null)
            return null;

        var callerId = currentUser.GetUserId();
        return session.IsParticipant(callerId) ? session : null;
    }
}

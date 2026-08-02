namespace Message.Web.API.Application.Queries.Sessions;

/// <summary>
/// 获取会话详情查询。
/// </summary>
/// <param name="SessionId">会话 ID</param>
public record GetSessionQuery(Guid SessionId) : IRequest<ChatSession?>;

/// <summary>
/// 获取会话详情查询处理程序。
/// <para>纯查询：不修改任何数据状态。</para>
/// <para>权限（修复 S-05）：仅会话参与者可查看会话详情，非参与者返回 null。</para>
/// </summary>
public class GetSessionQueryHandler(
    IChatSessionRepository sessionRepository,
    ICurrentUserService currentUser) : IRequestHandler<GetSessionQuery, ChatSession?>
{
    public async Task<ChatSession?> Handler(GetSessionQuery query, CancellationToken cancellationToken)
    {
        var session = await sessionRepository.GetByIdAsync(query.SessionId);
        if (session == null)
            return null;

        var callerId = currentUser.GetUserId();
        return session.IsParticipant(callerId) ? session : null;
    }
}

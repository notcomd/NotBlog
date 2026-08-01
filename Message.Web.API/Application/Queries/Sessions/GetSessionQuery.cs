namespace Message.Web.API.Application.Queries.Sessions;

/// <summary>
/// 获取会话详情查询。
/// </summary>
/// <param name="SessionId">会话 ID</param>
public record GetSessionQuery(Guid SessionId) : IRequest<ChatSession?>;

/// <summary>
/// 获取会话详情查询处理程序。
/// <para>纯查询：不修改任何数据状态。</para>
/// </summary>
public class GetSessionQueryHandler(
    IChatSessionRepository sessionRepository) : IRequestHandler<GetSessionQuery, ChatSession?>
{
    public async Task<ChatSession?> Handler(GetSessionQuery query, CancellationToken cancellationToken)
        => await sessionRepository.GetByIdAsync(query.SessionId);
}

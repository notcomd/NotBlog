using Message.Infrastructure.Services;

namespace Message.Web.API.Application.Commands.Sessions;

/// <summary>
/// 解散会话命令。
/// </summary>
/// <param name="SessionId">会话 ID</param>
public record DismissSessionCommand(Guid SessionId) : IRequest<bool>;

/// <summary>
/// 解散会话命令处理程序。
/// <para>权限（修复 S-05）：仅会话参与者可解散会话。</para>
/// </summary>
public class DismissSessionCommandHandler(
    IChatSessionRepository sessionRepository,
    ICurrentUserService currentUser,
    ILogger<DismissSessionCommandHandler> logger,
    SessionCacheService sessionCache) : IRequestHandler<DismissSessionCommand, bool>
{
    public async Task<bool> Handler(DismissSessionCommand command, CancellationToken cancellationToken)
    {
        var session = await sessionRepository.GetByIdAsync(command.SessionId);
        if (session == null)
            throw new KeyNotFoundException("会话不存在");

        var operatorId = currentUser.GetUserId();
        if (!session.IsParticipant(operatorId))
            throw new UnauthorizedAccessException("您不是该会话的参与者");

        session.Dismiss();
        await sessionRepository.UpdateAsync(session);
        await sessionRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        // Q-05：会话数据已变化，失效会话详情缓存
        await sessionCache.InvalidateSessionAsync(command.SessionId, cancellationToken);

        logger.LogInformation("会话 {SessionId} 已解散", command.SessionId);
        return true;
    }
}

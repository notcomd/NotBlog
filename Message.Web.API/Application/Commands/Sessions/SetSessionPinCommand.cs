using Message.Infrastructure.Services;

namespace Message.Web.API.Application.Commands.Sessions;

/// <summary>
/// 置顶/取消置顶会话命令。
/// </summary>
/// <param name="SessionId">会话 ID</param>
/// <param name="Pin">true 置顶，false 取消置顶</param>
public record SetSessionPinCommand(Guid SessionId, bool Pin) : IRequest<bool>;

/// <summary>
/// 置顶/取消置顶会话命令处理程序。
/// <para>权限（修复 S-05）：仅会话参与者可置顶会话。</para>
/// </summary>
public class SetSessionPinCommandHandler(
    IChatSessionRepository sessionRepository,
    ICurrentUserService currentUser,
    ILogger<SetSessionPinCommandHandler> logger,
    SessionCacheService sessionCache) : IRequestHandler<SetSessionPinCommand, bool>
{
    public async Task<bool> Handler(SetSessionPinCommand command, CancellationToken cancellationToken)
    {
        var session = await sessionRepository.GetByIdAsync(command.SessionId);
        if (session == null)
            throw new KeyNotFoundException("会话不存在");

        var operatorId = currentUser.GetUserId();
        if (!session.IsParticipant(operatorId))
            throw new UnauthorizedAccessException("您不是该会话的参与者");

        if (command.Pin)
            session.Pin();
        else
            session.Unpin();

        await sessionRepository.UpdateAsync(session);
        await sessionRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        // Q-05：会话数据已变化，失效会话详情缓存
        await sessionCache.InvalidateSessionAsync(command.SessionId, cancellationToken);

        logger.LogInformation("会话 {SessionId} 置顶状态已设置为 {Pin}", command.SessionId, command.Pin);
        return true;
    }
}

using Message.Infrastructure.Services;

namespace Message.Web.API.Application.Commands.Sessions;

/// <summary>
/// 向会话添加参与者命令。
/// </summary>
/// <param name="SessionId">会话 ID</param>
/// <param name="UserId">要添加的用户 ID</param>
public record AddSessionParticipantCommand(Guid SessionId, Guid UserId) : IRequest<bool>;

/// <summary>
/// 向会话添加参与者命令处理程序。
/// <para>权限（修复 S-05）：仅会话参与者可添加参与者。</para>
/// </summary>
public class AddSessionParticipantCommandHandler(
    IChatSessionRepository sessionRepository,
    ICurrentUserService currentUser,
    ILogger<AddSessionParticipantCommandHandler> logger,
    SessionCacheService sessionCache) : IRequestHandler<AddSessionParticipantCommand, bool>
{
    public async Task<bool> Handler(AddSessionParticipantCommand command, CancellationToken cancellationToken)
    {
        var session = await sessionRepository.GetByIdAsync(command.SessionId);
        if (session == null)
            throw new KeyNotFoundException("会话不存在");

        var operatorId = currentUser.GetUserId();
        if (!session.IsParticipant(operatorId))
            throw new UnauthorizedAccessException("您不是该会话的参与者");

        session.AddParticipant(command.UserId);
        await sessionRepository.UpdateAsync(session);
        await sessionRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        // Q-05：会话参与者已变化，失效会话详情缓存
        await sessionCache.InvalidateSessionAsync(command.SessionId, cancellationToken);

        logger.LogInformation("用户 {UserId} 已加入会话 {SessionId}", command.UserId, command.SessionId);
        return true;
    }
}

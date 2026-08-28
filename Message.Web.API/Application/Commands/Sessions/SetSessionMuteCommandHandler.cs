
namespace Message.Web.API.Application.Commands.Sessions;
/// <summary>
/// 静音/取消静音会话命令处理程序。
/// <para>权限（修复 S-05）：仅会话参与者可静音会话。</para>
/// </summary>
public class SetSessionMuteCommandHandler(
    IChatSessionRepository sessionRepository,
    ICurrentUserService currentUser,
    ILogger<SetSessionMuteCommandHandler> logger,
    SessionCacheService sessionCache) : IRequestHandler<SetSessionMuteCommand, bool>
{
    public async Task<bool> Handler(SetSessionMuteCommand command, CancellationToken cancellationToken)
    {
        var session = await sessionRepository.GetByIdAsync(command.SessionId);
        if (session == null)
            throw new KeyNotFoundException("会话不存在");

        var operatorId = currentUser.GetUserId();
        if (!session.IsParticipant(operatorId))
            throw new UnauthorizedAccessException("您不是该会话的参与者");

        session.SetMuted(operatorId, command.Mute);

        await sessionRepository.UpdateAsync(session);
        await sessionRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        // Q-05：会话数据已变化，失效会话详情缓存
        await sessionCache.InvalidateSessionAsync(command.SessionId, cancellationToken);

        logger.LogInformation("会话 {SessionId} 静音状态已设置为 {Mute}", command.SessionId, command.Mute);
        return true;
    }
}

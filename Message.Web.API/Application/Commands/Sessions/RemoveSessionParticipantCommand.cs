namespace Message.Web.API.Application.Commands.Sessions;

/// <summary>
/// 从会话移除参与者命令。
/// </summary>
/// <param name="SessionId">会话 ID</param>
/// <param name="UserId">要移除的用户 ID</param>
public record RemoveSessionParticipantCommand(Guid SessionId, Guid UserId) : IRequest<bool>;

/// <summary>
/// 从会话移除参与者命令处理程序。
/// </summary>
public class RemoveSessionParticipantCommandHandler(
    IChatSessionRepository sessionRepository,
    ILogger<RemoveSessionParticipantCommandHandler> logger) : IRequestHandler<RemoveSessionParticipantCommand, bool>
{
    public async Task<bool> Handler(RemoveSessionParticipantCommand command, CancellationToken cancellationToken)
    {
        var session = await sessionRepository.GetByIdAsync(command.SessionId);
        if (session == null)
            throw new KeyNotFoundException("会话不存在");

        session.RemoveParticipant(command.UserId);
        await sessionRepository.UpdateAsync(session);
        await sessionRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        logger.LogInformation("用户 {UserId} 已从会话 {SessionId} 移除", command.UserId, command.SessionId);
        return true;
    }
}

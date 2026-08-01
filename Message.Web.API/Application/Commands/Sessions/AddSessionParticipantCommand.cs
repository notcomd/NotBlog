namespace Message.Web.API.Application.Commands.Sessions;

/// <summary>
/// 向会话添加参与者命令。
/// </summary>
/// <param name="SessionId">会话 ID</param>
/// <param name="UserId">要添加的用户 ID</param>
public record AddSessionParticipantCommand(Guid SessionId, Guid UserId) : IRequest<bool>;

/// <summary>
/// 向会话添加参与者命令处理程序。
/// </summary>
public class AddSessionParticipantCommandHandler(
    IChatSessionRepository sessionRepository,
    ILogger<AddSessionParticipantCommandHandler> logger) : IRequestHandler<AddSessionParticipantCommand, bool>
{
    public async Task<bool> Handler(AddSessionParticipantCommand command, CancellationToken cancellationToken)
    {
        var session = await sessionRepository.GetByIdAsync(command.SessionId);
        if (session == null)
            throw new KeyNotFoundException("会话不存在");

        session.AddParticipant(command.UserId);
        await sessionRepository.UpdateAsync(session);
        await sessionRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        logger.LogInformation("用户 {UserId} 已加入会话 {SessionId}", command.UserId, command.SessionId);
        return true;
    }
}

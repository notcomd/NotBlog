namespace Message.Web.API.Application.Commands.Sessions;

/// <summary>
/// 解散会话命令。
/// </summary>
/// <param name="SessionId">会话 ID</param>
public record DismissSessionCommand(Guid SessionId) : IRequest<bool>;

/// <summary>
/// 解散会话命令处理程序。
/// </summary>
public class DismissSessionCommandHandler(
    IChatSessionRepository sessionRepository,
    ILogger<DismissSessionCommandHandler> logger) : IRequestHandler<DismissSessionCommand, bool>
{
    public async Task<bool> Handler(DismissSessionCommand command, CancellationToken cancellationToken)
    {
        var session = await sessionRepository.GetByIdAsync(command.SessionId);
        if (session == null)
            throw new KeyNotFoundException("会话不存在");

        session.Dismiss();
        await sessionRepository.UpdateAsync(session);
        await sessionRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        logger.LogInformation("会话 {SessionId} 已解散", command.SessionId);
        return true;
    }
}

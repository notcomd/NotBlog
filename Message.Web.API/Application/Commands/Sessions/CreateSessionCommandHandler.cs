namespace Message.Web.API.Application.Commands.Sessions;
/// <summary>
/// 创建会话命令处理程序。
/// </summary>
public class CreateSessionCommandHandler(
    IChatSessionRepository sessionRepository,
    ILogger<CreateSessionCommandHandler> logger) : IRequestHandler<CreateSessionCommand, Guid>
{
    public async Task<Guid> Handler(CreateSessionCommand command, CancellationToken cancellationToken)
    {
        ChatSession session;
        if (command.SessionType == SessionType.Private)
        {
            var existing = await sessionRepository.GetPrivateSessionAsync(command.UserId, command.FriendId!.Value);
            if (existing != null)
            {
                logger.LogInformation("私聊会话已存在：{SessionId}", existing.SessionId);
                return existing.SessionId;
            }

            session = ChatSession.CreatePrivateSession(command.UserId, command.FriendId!.Value);
        }
        else
        {
            session = ChatSession.CreateGroupSession(
                command.GroupId!.Value,
                command.UserId,
                command.InitialMembers ?? new HashSet<Guid>());
        }

        await sessionRepository.AddAsync(session);
        await sessionRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        logger.LogInformation("创建会话成功：{SessionId}，类型={SessionType}",
            session.SessionId, session.SessionType);
        return session.SessionId;
    }
}

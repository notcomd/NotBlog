
namespace Message.Web.API.Application.Commands.Sessions;
/// <summary>
/// 向会话添加参与者命令处理程序。
/// <para>权限（修复 S-05）：仅会话参与者可添加参与者。</para>
/// </summary>
public class AddSessionParticipantCommandHandler(
    IChatSessionRepository sessionRepository,
    ICurrentUserService currentUser,
    IUserInfoRepository userInfoRepository,
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

        // 设计文档 4.5：群聊会话人数上限 = 10 × 会话创建者等级 + 20（防绕过群成员管理的直加路径）
        if (session.SessionType == SessionType.Group)
        {
            var creatorInfo = await userInfoRepository.GetByUserIdAsync(session.CreatorId);
            var creatorLevel = creatorInfo?.Level ?? 1;
            var maxParticipants = 10 * creatorLevel + 20;
            if (session.Participants.Count >= maxParticipants)
                throw new InvalidOperationException($"会话人数已达上限（{maxParticipants} 人）");
        }

        session.AddParticipant(command.UserId);
        await sessionRepository.UpdateAsync(session);
        await sessionRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        // Q-05：会话参与者已变化，失效会话详情缓存
        await sessionCache.InvalidateSessionAsync(command.SessionId, cancellationToken);

        logger.LogInformation("用户 {UserId} 已加入会话 {SessionId}", command.UserId, command.SessionId);
        return true;
    }
}

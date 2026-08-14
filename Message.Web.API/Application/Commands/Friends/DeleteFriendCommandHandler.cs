namespace Message.Web.API.Application.Commands.Friends;
/// <summary>
/// 删除好友关系命令处理程序。
/// <para>联动（2026-08-15）：删除好友后同步解散双方私聊会话（软删 IsDismissed，历史消息保留；
/// 重新加好友时查重过滤已解散会话，自动开启全新会话）。</para>
/// </summary>
public class DeleteFriendCommandHandler(
    IMessageFriendsRepository friendRepository,
    IChatSessionRepository sessionRepository,
    ILogger<DeleteFriendCommandHandler> logger) : IRequestHandler<DeleteFriendCommand, bool>
{
    public async Task<bool> Handler(DeleteFriendCommand command, CancellationToken cancellationToken)
    {
        var friendship = await friendRepository.GetByUserAndFriendAsync(command.UserId, command.FriendId);
        if (friendship == null)
            throw new KeyNotFoundException("好友关系不存在");

        await friendRepository.DeleteAsync(friendship.FriendshipId);

        // 联动：同步解散双方私聊会话（软删，历史消息保留）
        var session = await sessionRepository.GetPrivateSessionAsync(command.UserId, command.FriendId);
        if (session is not null && !session.IsDismissed)
        {
            session.Dismiss();
            await sessionRepository.UpdateAsync(session);
        }

        await friendRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        logger.LogInformation("已删除好友关系：{FriendshipId}（用户 {UserId} 与 {FriendId}）",
            friendship.FriendshipId, command.UserId, command.FriendId);
        return true;
    }
}

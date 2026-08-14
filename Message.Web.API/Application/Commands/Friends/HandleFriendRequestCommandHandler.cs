namespace Message.Web.API.Application.Commands.Friends;
/// <summary>
/// 处理好友请求命令处理程序。
/// <para>联动（2026-08-15）：接受好友请求后自动创建双方私聊会话（查重复用；
/// 已解散的旧会话不算有效，会创建全新会话），客户端无需再单独调用创建会话接口。</para>
/// </summary>
public class HandleFriendRequestCommandHandler(
    IMessageFriendsRepository friendRepository,
    IChatSessionRepository sessionRepository,
    ILogger<HandleFriendRequestCommandHandler> logger) : IRequestHandler<HandleFriendRequestCommand, bool>
{
    public async Task<bool> Handler(HandleFriendRequestCommand command, CancellationToken cancellationToken)
    {
        if (command.Accept)
        {
            var friendship = await friendRepository.GetByUserAndFriendAsync(command.FriendId, command.UserId);
            if (friendship == null)
                throw new KeyNotFoundException("好友请求不存在");

            friendship.Accept();
            await friendRepository.UpdateAsync(friendship);

            // 联动：成为好友自动创建私聊会话（双方参与者，查重过滤已解散会话）
            var existing = await sessionRepository.GetPrivateSessionAsync(command.FriendId, command.UserId);
            if (existing is null)
            {
                var session = ChatSession.CreatePrivateSession(command.FriendId, command.UserId);
                await sessionRepository.AddAsync(session);
            }

            await friendRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        }
        else
        {
            var friendship = await friendRepository.GetByUserAndFriendAsync(command.FriendId, command.UserId);
            if (friendship == null)
                throw new KeyNotFoundException("好友请求不存在");

            friendship.Reject();
            await friendRepository.UpdateAsync(friendship);
            await friendRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        }

        logger.LogInformation("用户 {UserId} 已{Action}来自 {FriendId} 的好友请求",
            command.UserId, command.Accept ? "接受" : "拒绝", command.FriendId);
        return true;
    }
}

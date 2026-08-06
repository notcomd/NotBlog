namespace Message.Web.API.Application.Commands.Friends;
/// <summary>
/// 删除好友关系命令处理程序。
/// </summary>
public class DeleteFriendCommandHandler(
    IMessageFriendsRepository friendRepository,
    ILogger<DeleteFriendCommandHandler> logger) : IRequestHandler<DeleteFriendCommand, bool>
{
    public async Task<bool> Handler(DeleteFriendCommand command, CancellationToken cancellationToken)
    {
        var friendship = await friendRepository.GetByUserAndFriendAsync(command.UserId, command.FriendId);
        if (friendship == null)
            throw new KeyNotFoundException("好友关系不存在");

        await friendRepository.DeleteAsync(friendship.FriendshipId);
        await friendRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        logger.LogInformation("已删除好友关系：{FriendshipId}（用户 {UserId} 与 {FriendId}）",
            friendship.FriendshipId, command.UserId, command.FriendId);
        return true;
    }
}

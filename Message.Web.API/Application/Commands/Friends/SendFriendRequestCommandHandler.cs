namespace Message.Web.API.Application.Commands.Friends;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// 发送好友请求命令处理程序。
/// <para>发送前校验（避免触达唯一索引 (UserId, FriendId) 后仅得 500）：
/// 禁止添加自己、禁止重复向已是好友/已有待处理请求/已屏蔽的用户发送、
/// 禁止与对方的待处理请求形成双向 Pending；并发重复由唯一索引兜底并转为业务异常。</para>
/// </summary>
public class SendFriendRequestCommandHandler(
    IMessageFriendsRepository friendRepository,
    ILogger<SendFriendRequestCommandHandler> logger) : IRequestHandler<SendFriendRequestCommand, Guid>
{
    public async Task<Guid> Handler(SendFriendRequestCommand command, CancellationToken cancellationToken)
    {
        if (command.UserId == command.FriendId)
            throw new InvalidOperationException("不能添加自己为好友");

        await EnsureSendableAsync(command);

        var friendship = new MessageFriends(command.UserId, command.FriendId);
        await friendRepository.AddAsync(friendship);

        try
        {
            await friendRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // 并发重复发送：(UserId, FriendId) 唯一索引兜底
            logger.LogWarning("并发好友请求冲突已拦截：UserId={UserId}, FriendId={FriendId}",
                command.UserId, command.FriendId);
            throw new InvalidOperationException("好友请求已发送，等待对方处理");
        }

        logger.LogInformation("用户 {UserId} 向 {FriendId} 发送了好友请求，友谊关系 {FriendshipId}",
            command.UserId, command.FriendId, friendship.FriendshipId);
        return friendship.FriendshipId;
    }

    /// <summary>
    /// 发送前校验：同向既有关系按状态给出明确提示；反向存在待处理请求时提示直接同意，
    /// 避免同一对用户产生两条方向相反的 Pending 记录。
    /// </summary>
    private async Task EnsureSendableAsync(SendFriendRequestCommand command)
    {
        var existing = await friendRepository.GetByUserAndFriendAsync(command.UserId, command.FriendId);
        if (existing is not null)
        {
            throw existing.Status switch
            {
                FriendshipStatus.Accepted => new InvalidOperationException("对方已经是您的好友"),
                FriendshipStatus.Pending => new InvalidOperationException("好友请求已发送，等待对方处理"),
                FriendshipStatus.Blocked => new InvalidOperationException("已屏蔽该用户，请先解除屏蔽"),
                _ => new InvalidOperationException("对方已拒绝过您的好友请求")
            };
        }

        var reverse = await friendRepository.GetByUserAndFriendAsync(command.FriendId, command.UserId);
        if (reverse is { Status: FriendshipStatus.Pending })
            throw new InvalidOperationException("对方已向您发送好友请求，请直接同意");
    }
}

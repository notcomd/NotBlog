
namespace Message.Domain.Entities.Chat;

/// <summary>
///  好友关系
/// <para>
///  以 <see cref="Status"/> 为唯一状态源（状态机：Pending→Accepted/Rejected/Blocked，Blocked→Accepted），
///  屏蔽状态 <see cref="IsBlocked"/> 为只读派生属性，避免与 <see cref="Status"/> 双份状态导致不一致；
///  IsMuted/IsStarred 为双方独立的偏好标记，保留独立存储。
/// </para>
/// </summary>
public class MessageFriends : Entity<Guid>, IAggregateRoot
{
    public MessageFriends(Guid userId, Guid friendId) : this()
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("用户ID不能为空", nameof(userId));
        if (friendId == Guid.Empty)
            throw new ArgumentException("好友ID不能为空", nameof(friendId));
        if (userId == friendId)
            throw new ArgumentException("不能添加自己为好友");

        UserId = userId;
        FriendId = friendId;

        // 请求发起即发布领域事件：接收者侧生成「好友请求」站内通知（FriendshipCreatedEventHandler）
        AddDomainEvent(new FriendshipCreatedEvent(userId, friendId));
    }

    private MessageFriends()
    {
        FriendshipId = Guid.NewGuid();
        Status = FriendshipStatus.Pending;
        CreatedTime = DateTime.UtcNow;
        IsMuted = false;
        IsStarred = false;
    }

    public Guid FriendshipId { get; init; }

    public Guid UserId { get; init; }

    public Guid FriendId { get; init; }

    public FriendshipStatus Status { get; private set; }

    /// <summary>是否已屏蔽（由 <see cref="Status"/> 派生的只读视图）。</summary>
    public bool IsBlocked => Status == FriendshipStatus.Blocked;

    public string? Remark { get; private set; }

    public string? FriendGroupName { get; private set; }

    public bool IsMuted { get; private set; }

    public bool IsStarred { get; private set; }

    public DateTime CreatedTime { get; init; }

    public DateTime? AcceptedTime { get; private set; }

    public DateTime? LastInteractionTime { get; private set; }

    /// <summary>阻塞前状态（解封时恢复依据），未屏蔽时为 null。</summary>
    private FriendshipStatus? _preBlockStatus;


    public void Accept()
    {
        if (Status != FriendshipStatus.Pending)
            throw new InvalidOperationException("只有待处理的好友请求可以接受");

        Status = FriendshipStatus.Accepted;
        AcceptedTime = DateTime.UtcNow;
        AddDomainEvent(new FriendshipAcceptedEvent(UserId, FriendId));
    }

    public void Reject()
    {
        if (Status != FriendshipStatus.Pending)
            throw new InvalidOperationException("只有待处理的好友请求可以拒绝");

        Status = FriendshipStatus.Rejected;
        AddDomainEvent(new FriendshipRejectedEvent(UserId, FriendId));
    }

    public void Block()
    {
        if (Status == FriendshipStatus.Blocked)
            throw new InvalidOperationException("好友已被屏蔽");

        // 记录阻塞前状态，解封时准确恢复（防止把待处理/已拒绝状态错误改写为 Accepted）
        _preBlockStatus = Status;
        Status = FriendshipStatus.Blocked;
    }

    public void Unblock()
    {
        if (Status != FriendshipStatus.Blocked)
            throw new InvalidOperationException("好友未被屏蔽");

        Status = _preBlockStatus ?? FriendshipStatus.Accepted;
        _preBlockStatus = null;
    }

    public void Mute()
    {
        IsMuted = true;
    }

    public void Unmute()
    {
        IsMuted = false;
    }

    public void Star()
    {
        IsStarred = true;
    }

    public void Unstar()
    {
        IsStarred = false;
    }

    public void UpdateRemark(string? remark)
    {
        Remark = remark;
    }

    public void UpdateFriendGroup(string? groupName)
    {
        FriendGroupName = groupName;
    }

    public void RecordInteraction()
    {
        LastInteractionTime = DateTime.UtcNow;
    }

    public bool CanSendMessage()
    {
        return Status == FriendshipStatus.Accepted && !IsBlocked;
    }
}
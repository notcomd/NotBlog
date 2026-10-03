
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
    /// <summary>创建好友关系（发起好友请求，初始状态为待处理）</summary>
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
        FriendshipId = Guid.CreateVersion7();
        Status = FriendshipStatus.Pending;
        CreatedTime = DateTime.UtcNow;
        IsMuted = false;
        IsStarred = false;
    }

    /// <summary>好友关系ID</summary>
    public Guid FriendshipId { get; init; }

    /// <summary>发起方用户ID</summary>
    public Guid UserId { get; init; }

    /// <summary>好友用户ID</summary>
    public Guid FriendId { get; init; }

    /// <summary>好友关系状态</summary>
    public FriendshipStatus Status { get; private set; }

    /// <summary>是否已屏蔽（由 <see cref="Status"/> 派生的只读视图）。</summary>
    public bool IsBlocked => Status == FriendshipStatus.Blocked;

    /// <summary>好友备注</summary>
    public string? Remark { get; private set; }

    /// <summary>好友分组名称</summary>
    public string? FriendGroupName { get; private set; }

    /// <summary>是否免打扰</summary>
    public bool IsMuted { get; private set; }

    /// <summary>是否星标</summary>
    public bool IsStarred { get; private set; }

    /// <summary>创建时间</summary>
    public DateTime CreatedTime { get; init; }

    /// <summary>成为好友的时间</summary>
    public DateTime? AcceptedTime { get; private set; }

    /// <summary>最后互动时间</summary>
    public DateTime? LastInteractionTime { get; private set; }

    /// <summary>阻塞前状态（解封时恢复依据），未屏蔽时为 null。</summary>
    private FriendshipStatus? _preBlockStatus;


    /// <summary>接受好友请求</summary>
    public void Accept()
    {
        if (Status != FriendshipStatus.Pending)
            throw new InvalidOperationException("只有待处理的好友请求可以接受");

        Status = FriendshipStatus.Accepted;
        AcceptedTime = DateTime.UtcNow;
        AddDomainEvent(new FriendshipAcceptedEvent(UserId, FriendId));
    }

    /// <summary>拒绝好友请求</summary>
    public void Reject()
    {
        if (Status != FriendshipStatus.Pending)
            throw new InvalidOperationException("只有待处理的好友请求可以拒绝");

        Status = FriendshipStatus.Rejected;
        AddDomainEvent(new FriendshipRejectedEvent(UserId, FriendId));
    }

    /// <summary>屏蔽好友</summary>
    public void Block()
    {
        if (Status == FriendshipStatus.Blocked)
            throw new InvalidOperationException("好友已被屏蔽");

        // 记录阻塞前状态，解封时准确恢复（防止把待处理/已拒绝状态错误改写为 Accepted）
        _preBlockStatus = Status;
        Status = FriendshipStatus.Blocked;
    }

    /// <summary>解除屏蔽（恢复屏蔽前的状态）</summary>
    public void Unblock()
    {
        if (Status != FriendshipStatus.Blocked)
            throw new InvalidOperationException("好友未被屏蔽");

        Status = _preBlockStatus ?? FriendshipStatus.Accepted;
        _preBlockStatus = null;
    }

    /// <summary>设置免打扰</summary>
    public void Mute()
    {
        IsMuted = true;
    }

    /// <summary>取消免打扰</summary>
    public void Unmute()
    {
        IsMuted = false;
    }

    /// <summary>设置星标</summary>
    public void Star()
    {
        IsStarred = true;
    }

    /// <summary>取消星标</summary>
    public void Unstar()
    {
        IsStarred = false;
    }

    /// <summary>更新好友备注</summary>
    public void UpdateRemark(string? remark)
    {
        Remark = remark;
    }

    /// <summary>更新好友分组</summary>
    public void UpdateFriendGroup(string? groupName)
    {
        FriendGroupName = groupName;
    }

    /// <summary>记录互动时间</summary>
    public void RecordInteraction()
    {
        LastInteractionTime = DateTime.UtcNow;
    }

    /// <summary>判断当前是否为好友关系且未被屏蔽（可用于发送消息）</summary>
    public bool CanSendMessage()
    {
        return Status == FriendshipStatus.Accepted && !IsBlocked;
    }
}
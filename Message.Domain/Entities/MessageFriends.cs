using Message.Domain.Enums;
using Message.Domain.SeedWork;

namespace Message.Domain.Entities;

/// <summary>
///  好友关系
/// </summary>
public class MessageFriends : Entity, IAggregateRoot
{
    public MessageFriends(Guid userId, Guid friendId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("用户ID不能为空", nameof(userId));
        if (friendId == Guid.Empty)
            throw new ArgumentException("好友ID不能为空", nameof(friendId));
        if (userId == friendId)
            throw new ArgumentException("不能添加自己为好友");

        FriendshipId = Guid.NewGuid();
        UserId = userId;
        FriendId = friendId;
        Status = FriendshipStatus.Pending;
        CreatedTime = DateTime.UtcNow;
        IsBlocked = false;
        IsMuted = false;
        IsStarred = false;
    }

    private MessageFriends()
    {
        FriendshipId = Guid.NewGuid();
        Status = FriendshipStatus.Pending;
        CreatedTime = DateTime.UtcNow;
        IsBlocked = false;
        IsMuted = false;
        IsStarred = false;
    }

    public Guid FriendshipId { get; init; }

    public Guid UserId { get; init; }

    public Guid FriendId { get; init; }

    public FriendshipStatus Status { get; private set; }

    public string? Remark { get; set; }

    public string? FriendGroupName { get; set; }

    public bool IsBlocked { get; private set; }

    public bool IsMuted { get; private set; }

    public bool IsStarred { get; private set; }

    public DateTime CreatedTime { get; init; }

    public DateTime? AcceptedTime { get; private set; }

    public DateTime? LastInteractionTime { get; private set; }


    public void Accept()
    {
        if (Status != FriendshipStatus.Pending)
            throw new InvalidOperationException("只有待处理的好友请求可以接受");

        Status = FriendshipStatus.Accepted;
        AcceptedTime = DateTime.UtcNow;
    }

    public void Reject()
    {
        if (Status != FriendshipStatus.Pending)
            throw new InvalidOperationException("只有待处理的好友请求可以拒绝");

        Status = FriendshipStatus.Rejected;
    }

    public void Block()
    {
        if (IsBlocked)
            throw new InvalidOperationException("好友已被屏蔽");

        IsBlocked = true;
        Status = FriendshipStatus.Blocked;
    }

    public void Unblock()
    {
        if (!IsBlocked)
            throw new InvalidOperationException("好友未被屏蔽");

        IsBlocked = false;
        Status = FriendshipStatus.Accepted;
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
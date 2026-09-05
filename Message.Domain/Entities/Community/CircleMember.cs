
namespace Message.Domain.Entities.Community;

/// <summary>
/// 圈子成员
/// </summary>
public class CircleMember : Entity<Guid>
{
    private CircleMember()
    {
        CircleMembleGuid = Guid.CreateVersion7();
        JoinTime = DateTimeOffset.UtcNow;
    }

    public CircleMember(Guid circleGuid, Guid userGuid, string? nickname = null, CircleMemberRole role = CircleMemberRole.Member) : this()
    {
        if (circleGuid == Guid.Empty)
            throw new ArgumentException("圈子ID不能为空", nameof(circleGuid));
        if (userGuid == Guid.Empty)
            throw new ArgumentException("用户ID不能为空", nameof(userGuid));

        CircleGuid = circleGuid;
        UserGuid = userGuid;
        Role = role;
        Nickname = nickname;
        Status = CircleMemberStatus.Active;
    }
    public Guid CircleMembleGuid { get; init; }

    public Guid CircleGuid { get; private set; }

    public Guid UserGuid { get; private set; }

    public CircleMemberRole Role { get; private set; }

    public string? Nickname { get; private set; }

    public CircleMemberStatus Status { get; private set; }

    public DateTimeOffset JoinTime { get; private set; }

    public void SetRole(CircleMemberRole role)
    {
        Role = role;
    }

    public void SetNickname(string? nickname)
    {
        Nickname = nickname;
    }

    /// <summary>主动退出圈子</summary>
    public void MarkLeft()
    {
        Status = CircleMemberStatus.Left;
    }

    /// <summary>重新加入圈子（恢复 Active 状态）</summary>
    public void Reactivate()
    {
        Status = CircleMemberStatus.Active;
        JoinTime = DateTimeOffset.UtcNow;
    }

    /// <summary>被移出/封禁</summary>
    public void MarkBanned()
    {
        Status = CircleMemberStatus.Banned;
    }
}

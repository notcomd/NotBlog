
namespace Message.Domain.Entities.Community;

/// <summary>
/// 圈子成员
/// </summary>
public class CircleMember : Entity<Guid>
{
    private CircleMember()
    {
        CircleMemberGuid = Guid.CreateVersion7();
        JoinTime = DateTimeOffset.UtcNow;
    }

    /// <summary>创建圈子成员</summary>
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
    /// <summary>圈子成员ID</summary>
    public Guid CircleMemberGuid { get; init; }

    /// <summary>所属圈子ID</summary>
    public Guid CircleGuid { get; private set; }

    /// <summary>成员用户ID</summary>
    public Guid UserGuid { get; private set; }

    /// <summary>成员角色</summary>
    public CircleMemberRole Role { get; private set; }

    /// <summary>圈子内昵称</summary>
    public string? Nickname { get; private set; }

    /// <summary>成员状态</summary>
    public CircleMemberStatus Status { get; private set; }

    /// <summary>加入时间</summary>
    public DateTimeOffset JoinTime { get; private set; }

    /// <summary>设置成员角色</summary>
    public void SetRole(CircleMemberRole role)
    {
        Role = role;
    }

    /// <summary>设置圈子内昵称</summary>
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


namespace Message.Domain.Entities.Community;

/// <summary>
/// 圈子成员
/// </summary>
public class CircleMember : Entity<Guid>
{
    private CircleMember()
    {
        // 主键保持 Guid.Empty（IsTransient=true）：EF DetectChanges 才能识别为「新增」实体，
        // INSERT 时由 EF 生成主键。修复（2026-08-15）：此前此处生成 UUIDv7，导致加入聚合导航
        // 集合的新成员被 EF 判定为「已存在」（Modified）→ SaveChanges 生成 UPDATE 影响 0 行
        // → DbUpdateConcurrencyException（加入圈子失败）。
        JoinTime = DateTimeOffset.UtcNow;
    }

    public CircleMember(Guid circleGuid, Guid userGuid, CircleMemberRole role = CircleMemberRole.Member, string? nickname = null) : this()
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

    public Guid CircleGuid { get; private set; }

    public Guid UserGuid { get; private set; }

    public CircleMemberRole Role { get; private set; }
    /// <summary>圈内昵称（可空）</summary>
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

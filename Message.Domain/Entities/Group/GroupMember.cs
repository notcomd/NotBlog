
namespace Message.Domain.Entities.Group;

/// <summary>
///  群成员
/// </summary>
public class GroupMember : Entity<Guid>
{
    public GroupMember(Guid groupId, Guid userId, GroupMemberRole role) : this()
    {
        GroupId = groupId;
        UserId = userId;
        Role = role;
        JoinTime = DateTime.UtcNow;
        IsMuted = false;
        IsBanned = false;
    }

    private GroupMember()
    {
        // 主键保持 Guid.Empty（IsTransient=true）：EF DetectChanges 才能识别为「新增」实体，
        // INSERT 时由 EF 生成主键。修复（2026-08-15）：此前此处生成 UUIDv7，导致加入聚合导航
        // 集合的新成员被 EF 判定为「已存在」（Modified）→ SaveChanges 生成 UPDATE 影响 0 行
        // → DbUpdateConcurrencyException（加入群组失败）。
        JoinTime = DateTime.UtcNow;
    }

    public Guid MemberId { get; init; }

    public Guid GroupId { get; init; }

    public Guid UserId { get; init; }

    public GroupMemberRole Role { get; private set; }

    public string? Nickname { get; private set; }

    public DateTime JoinTime { get; init; }

    public DateTime? MuteEndTime { get; private set; }

    public bool IsMuted { get; private set; }

    public bool IsBanned { get; private set; }

    public DateTime? BannedTime { get; private set; }


    public void PromoteToAdmin()
    {
        if (Role == GroupMemberRole.Owner)
            throw new InvalidOperationException("群主不能更改角色");
        Role = GroupMemberRole.Admin;
    }

    public void DemoteToMember()
    {
        Role = GroupMemberRole.Member;
    }

    /// <summary>转让群主场景：将本成员角色置为群主（由聚合根 <see cref="Group.TransferOwnership"/> 调用）。</summary>
    public void BecomeOwner()
    {
        Role = GroupMemberRole.Owner;
    }

    public void Mute(TimeSpan duration)
    {
        IsMuted = true;
        MuteEndTime = DateTime.UtcNow.Add(duration);
    }

    /// <summary>
    ///  禁言成员到指定时间
    /// </summary>
    /// <param name="endTime">禁言结束时间，null表示永久禁言</param>
    public void Mute(DateTime? endTime)
    {
        IsMuted = true;
        MuteEndTime = endTime;
    }

    public void Unmute()
    {
        IsMuted = false;
        MuteEndTime = null;
    }

    public void Ban()
    {
        IsBanned = true;
        BannedTime = DateTime.UtcNow;
    }

    public void Unban()
    {
        IsBanned = false;
        BannedTime = null;
    }

    public bool CanSendMessage()
    {
        if (IsBanned) return false;
        if (IsMuted && MuteEndTime.HasValue && DateTime.UtcNow < MuteEndTime.Value)
            return false;
        return true;
    }

    /// <summary>
    /// 邀请成员：允许成员邀请时所有成员可邀请，否则需非普通成员。
    /// </summary>
    public bool CanInvite(bool allowMemberInvite)
    {
        return allowMemberInvite || Role != GroupMemberRole.Member;
    }

    /// <summary>
    /// 编辑群信息：允许成员编辑时所有成员可编辑，否则需非普通成员。
    /// </summary>
    public bool CanEditInfo(bool allowMemberEditInfo)
    {
        return allowMemberEditInfo || Role != GroupMemberRole.Member;
    }

    /// <summary>移除成员：需非普通成员。</summary>
    public bool CanRemoveMember() => Role != GroupMemberRole.Member;

    /// <summary>禁言/解禁成员：需群主或管理员。</summary>
    public bool CanMuteMember() => Role is GroupMemberRole.Admin or GroupMemberRole.Owner;

    /// <summary>封禁/解封成员：需群主或管理员。</summary>
    public bool CanBanMember() => Role is GroupMemberRole.Admin or GroupMemberRole.Owner;

    /// <summary>转让群主：仅群主。</summary>
    public bool CanTransferOwnership() => Role == GroupMemberRole.Owner;

    /// <summary>
    /// 按权限判定（角色能力内聚于此，聚合根 <see cref="Group.HasPermission"/> 委托本方法）。
    /// </summary>
    public bool Can(GroupPermission permission, bool allowMemberInvite, bool allowMemberEditInfo)
    {
        return permission switch
        {
            GroupPermission.SendMessage => CanSendMessage(),
            GroupPermission.InviteMember => CanInvite(allowMemberInvite),
            GroupPermission.EditGroupInfo => CanEditInfo(allowMemberEditInfo),
            GroupPermission.RemoveMember => CanRemoveMember(),
            GroupPermission.MuteMember => CanMuteMember(),
            GroupPermission.BanMember => CanBanMember(),
            GroupPermission.TransferOwnership => CanTransferOwnership(),
            _ => false
        };
    }

    /// <summary>
    ///  设置群昵称
    /// </summary>
    /// <param name="nickname">群昵称</param>
    public void SetNickname(string nickname)
    {
        Nickname = nickname;
    }
}
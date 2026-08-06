
namespace Message.Domain.Entities.Group;

/// <summary>
///   群聊
/// </summary>
public class Group : Entity<Guid>, IAggregateRoot
{
    /// <summary>
    ///   群成员
    /// </summary>
    private readonly List<GroupMember> _members = new();


    public Group(Guid ownerId, string groupName, int maxMembers = 500, bool isPublic = false) : this()
    {
        if (string.IsNullOrWhiteSpace(groupName))
            throw new ArgumentException("群名称不能为空", nameof(groupName));
        if (maxMembers <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxMembers), "最大成员数必须大于0");

        OwnerId = ownerId;
        GroupName = groupName;
        MaxMembers = maxMembers;
        IsPublic = isPublic;
        CreatedTime = DateTime.UtcNow;
        IsDismissed = false;
        AllowMemberInvite = true;
        AllowMemberEditInfo = false;

        var owner = new GroupMember(GroupId, ownerId, GroupMemberRole.Owner);
        _members.Add(owner);
    }

    /// <summary>
    ///   群构造函数
    /// </summary>
    private Group()
    {
        GroupId = Guid.NewGuid();
        CreatedTime = DateTime.UtcNow;
    }

    /// <summary>
    ///   群ID
    /// </summary>
    public Guid GroupId { get; init; }

    /// <summary>
    ///   群名称
    /// </summary>
    public string GroupName { get; private set; } = null!;

    /// <summary>
    ///   群描述
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    ///   群主ID
    /// </summary>
    public Guid OwnerId { get; private set; }

    /// <summary>
    ///   群头像
    /// </summary>
    public Uri? Avatar { get; private set; }

    /// <summary>
    ///   最大成员数
    /// </summary>
    public int MaxMembers { get; private set; }

    /// <summary>
    ///   是否公开
    /// </summary>
    public bool IsPublic { get; private set; }

    /// <summary>
    ///   是否允许成员邀请
    /// </summary>
    public bool AllowMemberInvite { get; private set; }

    /// <summary>
    ///   是否允许成员编辑群信息
    /// </summary>
    public bool AllowMemberEditInfo { get; private set; }

    /// <summary>
    ///   创建时间
    /// </summary>
    public DateTime CreatedTime { get; init; }

    /// <summary>
    ///   解散时间
    /// </summary>
    public DateTime? DismissedTime { get; private set; }

    /// <summary>
    ///   是否解散
    /// </summary>
    public bool IsDismissed { get; private set; }

    /// <summary>
    ///   群成员
    /// </summary>
    public IReadOnlyCollection<GroupMember> Members => _members.AsReadOnly();

    /// <summary>
    ///   群成员数
    /// </summary>
    public int MemberCount => _members.Count;

    /// <summary>
    ///   更新群信息
    /// </summary>
    public void UpdateGroupInfo(string groupName, string? description, Uri? avatar)
    {
        if (IsDismissed)
            throw new InvalidOperationException("群已解散");

        if (!string.IsNullOrWhiteSpace(groupName))
            GroupName = groupName;
        Description = description;
        Avatar = avatar;
    }

    /// <summary>
    /// 更新群描述
    /// </summary>
    /// <param name="description">群描述</param>
    public void UpdateDescription(string description)
    {
        if (IsDismissed)
            throw new InvalidOperationException("群已解散");
        Description = description;
    }

    /// <summary>
    /// 更新群头像
    /// </summary>
    /// <param name="avatar">群头像URI</param>
    public void UpdateAvatar(Uri avatar)
    {
        if (IsDismissed)
            throw new InvalidOperationException("群已解散");
        Avatar = avatar;
    }

    /// <summary>
    /// 更新群权限
    /// </summary>
    /// <param name="allowMemberInvite"></param>
    /// <param name="allowMemberEditInfo"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public void UpdatePermissions(bool allowMemberInvite, bool allowMemberEditInfo)
    {
        if (IsDismissed)
            throw new InvalidOperationException("群已解散");

        AllowMemberInvite = allowMemberInvite;
        AllowMemberEditInfo = allowMemberEditInfo;
    }

    /// <summary>
    ///   添加群成员
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="role">角色</param>
    /// <exception cref="InvalidOperationException"></exception>
    public void AddMember(Guid userId, GroupMemberRole role = GroupMemberRole.Member)
    {
        if (IsDismissed)
            throw new InvalidOperationException("群已解散");
        if (_members.Count >= MaxMembers)
            throw new InvalidOperationException("群成员已达上限");
        if (_members.Any(m => m.UserId == userId))
            throw new InvalidOperationException("用户已在群中");
        var member = new GroupMember(GroupId, userId, role);
        AddDomainEvent(new GroupMemberJoinedEvent(this.GroupId, member.UserId, member.Role));
        _members.Add(member);
    }

    /// <summary>
    ///   移除群成员
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <exception cref="InvalidOperationException"></exception>
    public void RemoveMember(Guid userId)
    {
        if (IsDismissed)
            throw new InvalidOperationException("群已解散");

        var member = _members.FirstOrDefault(m => m.UserId == userId);
        if (member is null)
            throw new KeyNotFoundException("成员不存在");

        if (member.Role == GroupMemberRole.Owner)
            throw new InvalidOperationException("不能移除群主");
        AddDomainEvent(new GroupMemberRemovedEvent(this.GroupId, member.UserId));
        _members.Remove(member);
    }

    /// <summary>
    ///   提升成员为管理员
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <exception cref="KeyNotFoundException">成员不存在时抛出</exception>
    public void PromoteMember(Guid userId)
    {
        if (IsDismissed)
            throw new InvalidOperationException("群已解散");

        var member = _members.FirstOrDefault(m => m.UserId == userId)
                     ?? throw new KeyNotFoundException("成员不存在");
        member.PromoteToAdmin();
    }

    /// <summary>
    ///   降级管理员为普通成员
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <exception cref="KeyNotFoundException">成员不存在时抛出</exception>
    public void DemoteMember(Guid userId)
    {
        if (IsDismissed)
            throw new InvalidOperationException("群已解散");

        var member = _members.FirstOrDefault(m => m.UserId == userId)
                     ?? throw new KeyNotFoundException("成员不存在");
        member.DemoteToMember();
    }

    /// <summary>
    ///   禁言成员
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="duration">禁言时长</param>
    /// <exception cref="KeyNotFoundException">成员不存在时抛出</exception>
    public void MuteMember(Guid userId, TimeSpan duration)
    {
        if (IsDismissed)
            throw new InvalidOperationException("群已解散");

        var member = _members.FirstOrDefault(m => m.UserId == userId)
                     ?? throw new KeyNotFoundException("成员不存在");
        member.Mute(duration);
    }

    /// <summary>
    ///   解除成员禁言
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <exception cref="KeyNotFoundException">成员不存在时抛出</exception>
    public void UnmuteMember(Guid userId)
    {
        if (IsDismissed)
            throw new InvalidOperationException("群已解散");

        var member = _members.FirstOrDefault(m => m.UserId == userId)
                     ?? throw new KeyNotFoundException("成员不存在");
        member.Unmute();
    }

    /// <summary>
    ///   封禁成员
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <exception cref="KeyNotFoundException">成员不存在时抛出</exception>
    public void BanMember(Guid userId)
    {
        if (IsDismissed)
            throw new InvalidOperationException("群已解散");

        var member = _members.FirstOrDefault(m => m.UserId == userId)
                     ?? throw new KeyNotFoundException("成员不存在");
        member.Ban();
    }

    /// <summary>
    ///   解封成员
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <exception cref="KeyNotFoundException">成员不存在时抛出</exception>
    public void UnbanMember(Guid userId)
    {
        if (IsDismissed)
            throw new InvalidOperationException("群已解散");

        var member = _members.FirstOrDefault(m => m.UserId == userId)
                     ?? throw new KeyNotFoundException("成员不存在");
        member.Unban();
    }

    /// <summary>
    ///   转让群主
    /// </summary>
    /// <param name="newOwnerId">新群主ID</param>
    /// <exception cref="InvalidOperationException"></exception>
    public void TransferOwnership(Guid newOwnerId)
    {
        if (IsDismissed)
            throw new InvalidOperationException("群已解散");

        var oldOwner = _members.FirstOrDefault(m => m.UserId == OwnerId);
        var newOwner = _members.FirstOrDefault(m => m.UserId == newOwnerId);

        if (oldOwner is null || newOwner is null)
            throw new KeyNotFoundException("成员不存在");

        oldOwner.DemoteToMember();
        newOwner.PromoteToAdmin();
        OwnerId = newOwnerId;
        AddDomainEvent(new GroupOwnershipTransferredEvent(GroupId, oldOwner.UserId, newOwnerId));
    }

    /// <summary>
    ///   解散群
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public void Dismiss()
    {
        if (IsDismissed)
            throw new InvalidOperationException("群已解散");

        IsDismissed = true;
        DismissedTime = DateTime.UtcNow;
        AddDomainEvent(new GroupDissolvedEvent(GroupId, OwnerId, DateTime.UtcNow));
    }

    /// <summary>
    ///   获取群成员
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>群成员</returns>
    /// <exception cref="KeyNotFoundException"></exception>
    public GroupMember? GetMember(Guid userId)
    {
        return _members.FirstOrDefault(m => m.UserId == userId);
    }

    /// <summary>
    ///   检查用户是否为群成员
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>是否为群成员</returns>
    public bool IsMember(Guid userId)
    {
        return _members.Any(m => m.UserId == userId);
    }

    /// <summary>
    ///   检查用户是否具有群权限
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="permission">权限</param>
    /// <returns>是否具有群权限</returns>
    /// <exception cref="KeyNotFoundException"></exception>
    public bool HasPermission(Guid userId, GroupPermission permission)
    {
        var member = GetMember(userId);
        if (member is null) return false;

        return permission switch
        {
            GroupPermission.SendMessage => member.CanSendMessage(),
            GroupPermission.InviteMember => AllowMemberInvite || member.Role != GroupMemberRole.Member,
            GroupPermission.EditGroupInfo => AllowMemberEditInfo || member.Role != GroupMemberRole.Member,
            GroupPermission.RemoveMember => member.Role != GroupMemberRole.Member,
            GroupPermission.MuteMember => member.Role == GroupMemberRole.Admin || member.Role == GroupMemberRole.Owner,
            GroupPermission.BanMember => member.Role == GroupMemberRole.Admin || member.Role == GroupMemberRole.Owner,
            GroupPermission.TransferOwnership => member.Role == GroupMemberRole.Owner,
            _ => false
        };
    }
}
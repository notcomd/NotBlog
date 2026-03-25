using Message.Domain.Enums;
using Message.Domain.Events;
using Message.Domain.SeedWork;

namespace Message.Domain.Entities.Group;

/// <summary>
///   群
/// </summary>
public class Group : Entity, IAggregateRoot
{
    private readonly List<GroupMember> _members = new();

    public Group(Guid ownerId, string groupName, int maxMembers = 500, bool isPublic = false)
    {
        if (string.IsNullOrWhiteSpace(groupName))
            throw new ArgumentException("群名称不能为空", nameof(groupName));
        if (maxMembers <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxMembers), "最大成员数必须大于0");

        GroupId = Guid.NewGuid();
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

    private Group()
    {
        GroupId = Guid.NewGuid();
        CreatedTime = DateTime.UtcNow;
    }

    public Guid GroupId { get; init; }
    public string GroupName { get; private set; }
    public string? Description { get; set; }
    public Guid OwnerId { get; private set; }
    public Uri? Avatar { get; set; }
    public int MaxMembers { get; private set; }
    public bool IsPublic { get; private set; }
    public bool AllowMemberInvite { get; private set; }
    public bool AllowMemberEditInfo { get; private set; }
    public DateTime CreatedTime { get; init; }
    public DateTime? DismissedTime { get; private set; }
    public bool IsDismissed { get; private set; }
    public IReadOnlyCollection<GroupMember> Members => _members.AsReadOnly();

    public int MemberCount => _members.Count;

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

    public void RemoveMember(Guid userId)
    {
        if (IsDismissed)
            throw new InvalidOperationException("群已解散");

        var member = _members.FirstOrDefault(m => m.UserId == userId);
        if (member is null)
            throw new KeyNotFoundException("成员不存在");

        if (member.Role == GroupMemberRole.Owner)
            throw new InvalidOperationException("不能移除群主");
        // AddDomainEvent(new GroupMemberRemovedEvent(this.GroupId, member.UserId));
        _members.Remove(member);
    }

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
    }

    public void Dismiss()
    {
        if (IsDismissed)
            throw new InvalidOperationException("群已解散");

        IsDismissed = true;
        DismissedTime = DateTime.UtcNow;
    }

    public GroupMember? GetMember(Guid userId)
    {
        return _members.FirstOrDefault(m => m.UserId == userId);
    }

    public bool IsMember(Guid userId)
    {
        return _members.Any(m => m.UserId == userId);
    }

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
            GroupPermission.BanMember => member.Role == GroupMemberRole.Admin || member.Role == GroupMemberRole.Owner,
            GroupPermission.TransferOwnership => member.Role == GroupMemberRole.Owner,
            _ => false
        };
    }
}
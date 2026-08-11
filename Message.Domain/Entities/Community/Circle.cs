
namespace Message.Domain.Entities.Community;

/// <summary>
/// 兴趣圈子（聚合根，Discord 式"服务器"）。
/// <para>
/// 邀请制社区：圈主创建圈子即成为 Owner 成员；成员通过邀请码 / 邀请链接 / 直邀确认加入；
/// 圈内帖子（Tweet.CircleGuid）仅成员可见、成员可互动。
/// </para>
/// </summary>
public class Circle : Entity<Guid>, IAggregateRoot
{
    private readonly List<CircleMember> _members = [];

    private Circle()
    {
        CircleGuid = Guid.CreateVersion7();
        CreateTime = DateTimeOffset.UtcNow;
    }

    /// <summary>创建圈子（圈主自动成为 Owner 成员）</summary>
    public static Circle Create(Guid ownerGuid, string name, string? description = null, string? avatarUrl = null, string? coverUrl = null, int maxMembers = 500)
    {
        if (ownerGuid == Guid.Empty)
            throw new ArgumentException("圈主ID不能为空", nameof(ownerGuid));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("圈子名称不能为空", nameof(name));
        if (name.Length > 50)
            throw new ArgumentException("圈子名称不能超过50个字符", nameof(name));
        if (maxMembers <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxMembers), "最大成员数必须大于0");

        var circle = new Circle
        {
            OwnerGuid = ownerGuid,
            Name = name.Trim(),
            Description = description,
            AvatarUrl = avatarUrl,
            CoverUrl = coverUrl,
            MaxMembers = maxMembers,
            MemberCount = 1,
            Status = CircleStatus.Active
        };

        circle._members.Add(new CircleMember(circle.CircleGuid, ownerGuid, CircleMemberRole.Owner));
        circle.AddDomainEvent(new CircleCreatedEvent(circle.CircleGuid, ownerGuid));
        return circle;
    }

    public Guid CircleGuid { get; init; }
    public Guid OwnerGuid { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public string? AvatarUrl { get; private set; }
    public string? CoverUrl { get; private set; }
    public int MaxMembers { get; private set; }
    /// <summary>成员数（冗余计数，与 _members 同步维护）</summary>
    public int MemberCount { get; private set; }
    public CircleStatus Status { get; private set; }
    public DateTimeOffset CreateTime { get; init; }
    public DateTimeOffset? DissolvedTime { get; private set; }

    public IReadOnlyCollection<CircleMember> Members => _members.AsReadOnly();

    public bool IsDissolved => Status == CircleStatus.Dissolved;

    /// <summary>更新圈子信息（圈主/管理员）</summary>
    public void UpdateInfo(string name, string? description, string? avatarUrl, string? coverUrl = null)
    {
        EnsureActive();
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("圈子名称不能为空", nameof(name));
        if (name.Length > 50)
            throw new ArgumentException("圈子名称不能超过50个字符", nameof(name));

        Name = name.Trim();
        Description = description;
        AvatarUrl = avatarUrl;
        CoverUrl = coverUrl;
    }

    /// <summary>解散圈子（仅圈主）</summary>
    public void Dissolve()
    {
        EnsureActive();
        Status = CircleStatus.Dissolved;
        DissolvedTime = DateTimeOffset.UtcNow;
        AddDomainEvent(new CircleDissolvedEvent(CircleGuid, OwnerGuid));
    }

    /// <summary>添加成员（邀请码/链接/直邀确认后调用）</summary>
    public void AddMember(Guid userGuid, CircleMemberRole role = CircleMemberRole.Member, string? nickname = null, Guid? inviterGuid = null)
    {
        EnsureActive();
        if (userGuid == Guid.Empty)
            throw new ArgumentException("用户ID不能为空", nameof(userGuid));
        if (_members.Count >= MaxMembers)
            throw new InvalidOperationException("圈子成员已达上限");
        if (_members.Any(m => m.UserGuid == userGuid && m.Status == CircleMemberStatus.Active))
            throw new InvalidOperationException("用户已在圈子中");

        // 重新加入：恢复被移出/退出的成员为 Active，不再重复入账
        var existing = _members.FirstOrDefault(m => m.UserGuid == userGuid);
        if (existing is not null)
        {
            existing.Reactivate();
            existing.SetRole(role);
            existing.SetNickname(nickname);
            AddDomainEvent(new CircleMemberJoinedEvent(CircleGuid, userGuid, role, inviterGuid));
            return;
        }

        var member = new CircleMember(CircleGuid, userGuid, role, nickname);
        _members.Add(member);
        MemberCount++;
        AddDomainEvent(new CircleMemberJoinedEvent(CircleGuid, userGuid, role, inviterGuid));
    }

    /// <summary>成员主动退出圈子</summary>
    public void RemoveMember(Guid userGuid)
    {
        EnsureActive();
        var member = GetActiveMember(userGuid)
            ?? throw new KeyNotFoundException("成员不存在或已退出");
        if (member.Role == CircleMemberRole.Owner)
            throw new InvalidOperationException("圈主不能退出圈子，请先转移圈主");

        member.MarkLeft();
        MemberCount = Math.Max(0, MemberCount - 1);
        AddDomainEvent(new CircleMemberLeftEvent(CircleGuid, userGuid));
    }

    /// <summary>移出/封禁成员（圈主/管理员）</summary>
    public void BanMember(Guid userGuid, Guid operatorGuid)
    {
        EnsureActive();
        var member = GetActiveMember(userGuid)
            ?? throw new KeyNotFoundException("成员不存在或已退出");
        if (member.Role == CircleMemberRole.Owner)
            throw new InvalidOperationException("不能移出圈主");
        if (userGuid == operatorGuid)
            throw new InvalidOperationException("不能移出自己");

        member.MarkBanned();
        MemberCount = Math.Max(0, MemberCount - 1);
        AddDomainEvent(new CircleMemberRemovedEvent(CircleGuid, userGuid, operatorGuid));
    }

    /// <summary>设置/取消管理员（仅圈主）</summary>
    public void SetMemberRole(Guid userGuid, CircleMemberRole role, Guid operatorGuid)
    {
        EnsureActive();
        if (role == CircleMemberRole.Owner)
            throw new InvalidOperationException("请使用转移圈主功能");
        var member = GetActiveMember(userGuid)
            ?? throw new KeyNotFoundException("成员不存在或已退出");
        if (operatorGuid != OwnerGuid)
            throw new UnauthorizedAccessException("只有圈主可以设置管理员");

        var oldRole = member.Role;
        if (oldRole == role)
            return;
        member.SetRole(role);
        AddDomainEvent(new CircleMemberRoleChangedEvent(CircleGuid, userGuid, oldRole, role, operatorGuid));
    }

    /// <summary>转移圈主（仅现任圈主）</summary>
    public void TransferOwnership(Guid newOwnerGuid, Guid operatorGuid)
    {
        EnsureActive();
        if (operatorGuid != OwnerGuid)
            throw new UnauthorizedAccessException("只有圈主可以转移圈主");
        var member = GetActiveMember(newOwnerGuid)
            ?? throw new KeyNotFoundException("新圈主必须是圈子成员");

        var oldOwner = GetActiveMember(OwnerGuid);
        oldOwner?.SetRole(CircleMemberRole.Member);
        member.SetRole(CircleMemberRole.Owner);
        OwnerGuid = newOwnerGuid;
        AddDomainEvent(new CircleOwnerTransferredEvent(CircleGuid, operatorGuid, newOwnerGuid));
    }

    /// <summary>判断用户是否为圈子有效成员</summary>
    public bool IsMember(Guid userGuid) => GetActiveMember(userGuid) is not null;

    /// <summary>获取有效成员</summary>
    public CircleMember? GetActiveMember(Guid userGuid)
        => _members.FirstOrDefault(m => m.UserGuid == userGuid && m.Status == CircleMemberStatus.Active);

    private void EnsureActive()
    {
        if (IsDissolved)
            throw new InvalidOperationException("圈子已解散");
    }
}


namespace Message.Domain.Entities.Community;

/// <summary>
/// 圈子邀请（聚合根）：邀请码 / 邀请链接 / 按用户直邀 三种形态。
/// <para>默认有效期 7 天；邀请码全局唯一（命令层生成时查重）。</para>
/// </summary>
public class CircleInvitation : Entity<Guid>, IAggregateRoot
{
    /// <summary>默认有效期：7 天</summary>
    public static readonly TimeSpan DefaultTtl = TimeSpan.FromDays(7);

    private CircleInvitation()
    {
        InviteGuid = Guid.CreateVersion7();
        CreateTime = DateTimeOffset.UtcNow;
    }

    /// <summary>创建邀请码邀请（code 由命令层生成并查重后传入）</summary>
    public static CircleInvitation CreateCode(Guid circleGuid, Guid inviterGuid, string code, TimeSpan? ttl = null)
    {
        var invite = CreateBase(circleGuid, inviterGuid, CircleInvitationType.Code, null, ttl);
        if (string.IsNullOrWhiteSpace(code) || code.Length != 6)
            throw new ArgumentException("邀请码必须为6位", nameof(code));
        invite.Code = code.ToUpperInvariant();
        return invite;
    }

    /// <summary>创建邀请链接（token 免输入加入）</summary>
    public static CircleInvitation CreateLink(Guid circleGuid, Guid inviterGuid, TimeSpan? ttl = null)
    {
        var invite = CreateBase(circleGuid, inviterGuid, CircleInvitationType.Link, null, ttl);
        invite.Token = Guid.CreateVersion7();
        return invite;
    }

    /// <summary>创建按用户直邀（对方确认后加入）</summary>
    public static CircleInvitation CreateDirect(Guid circleGuid, Guid inviterGuid, Guid inviteeGuid, TimeSpan? ttl = null)
    {
        var invite = CreateBase(circleGuid, inviterGuid, CircleInvitationType.Direct, inviteeGuid, ttl);
        return invite;
    }

    private static CircleInvitation CreateBase(
        Guid circleGuid, Guid inviterGuid, CircleInvitationType type, Guid? inviteeGuid, TimeSpan? ttl)
    {
        if (circleGuid == Guid.Empty)
            throw new ArgumentException("圈子ID不能为空", nameof(circleGuid));
        if (inviterGuid == Guid.Empty)
            throw new ArgumentException("邀请人ID不能为空", nameof(inviterGuid));
        if (type == CircleInvitationType.Direct && (inviteeGuid is null || inviteeGuid == Guid.Empty))
            throw new ArgumentException("直邀必须指定被邀请人", nameof(inviteeGuid));

        return new CircleInvitation
        {
            CircleGuid = circleGuid,
            InviterGuid = inviterGuid,
            InviteeGuid = inviteeGuid,
            Type = type,
            Status = CircleInvitationStatus.Pending,
            ExpireTime = DateTimeOffset.UtcNow + (ttl ?? DefaultTtl)
        };
    }

    public Guid InviteGuid { get; init; }
    public Guid CircleGuid { get; private set; }
    public Guid InviterGuid { get; private set; }
    /// <summary>直邀对象（邀请码/链接为 null）</summary>
    public Guid? InviteeGuid { get; private set; }
    /// <summary>6 位邀请码（全局唯一）</summary>
    public string? Code { get; private set; }
    /// <summary>邀请链接 token</summary>
    public Guid? Token { get; private set; }
    public CircleInvitationType Type { get; private set; }
    public CircleInvitationStatus Status { get; private set; }
    public DateTimeOffset ExpireTime { get; private set; }
    public DateTimeOffset CreateTime { get; init; }

    /// <summary>是否仍可被使用（Pending 且未过期）</summary>
    public bool IsValid() => Status == CircleInvitationStatus.Pending && ExpireTime > DateTimeOffset.UtcNow;

    /// <summary>接受邀请（直邀确认 / 码与链接加入时调用）</summary>
    public void Accept()
    {
        if (!IsValid())
            throw new InvalidOperationException("邀请已失效（已使用、已撤销或已过期）");
        Status = CircleInvitationStatus.Accepted;
        AddDomainEvent(new CircleInvitationAcceptedEvent(InviteGuid, CircleGuid, InviteeGuid ?? Guid.Empty));
    }

    /// <summary>撤销邀请（圈主/管理员撤销，或直邀被邀请人拒绝）</summary>
    public void Revoke(Guid operatorGuid)
    {
        if (Status != CircleInvitationStatus.Pending)
            throw new InvalidOperationException("只有待使用的邀请才能撤销");
        Status = CircleInvitationStatus.Revoked;
        AddDomainEvent(new CircleInvitationRevokedEvent(InviteGuid, CircleGuid, operatorGuid));
    }

    /// <summary>标记过期（惰性清理）</summary>
    public void MarkExpired()
    {
        Status = CircleInvitationStatus.Expired;
    }
}

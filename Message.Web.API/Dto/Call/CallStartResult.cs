namespace Message.Web.API.Dto.Call;

/// <summary>
/// <see cref="CallHub.StartCall"/> 的返回值。
/// </summary>
public sealed class CallStartResult
{
    /// <summary>通话 ID</summary>
    public Guid CallId { get; init; }

    /// <summary>发起通话的会话 ID</summary>
    public Guid SessionId { get; init; }

    /// <summary>通话类型</summary>
    public CallType Type { get; init; }

    /// <summary>通话形态（即时呼叫 / 常驻房间）</summary>
    public CallRoomKind RoomKind { get; init; }

    /// <summary>通话初始状态（房间模式为 Active；即时呼叫为 Ringing）</summary>
    public CallStatus Status { get; init; }

    /// <summary>呼叫方用户 ID</summary>
    public Guid CallerId { get; init; }

    /// <summary>会话全部参与者（含呼叫方）</summary>
    public List<Guid> Participants { get; init; } = [];

    /// <summary>发起时被邀请（收到来电）的成员子集；空表示未邀请任何人</summary>
    public List<Guid> InvitedMembers { get; init; } = [];

    /// <summary>已接通（进入通话）的成员</summary>
    public List<Guid> JoinedMembers { get; init; } = [];

    /// <summary>房间是否设置了入会密码</summary>
    public bool RequiresPassword { get; init; }

    /// <summary>忙线成员（无法接听来电，UI 可提示）</summary>
    public List<Guid> BusyUsers { get; init; } = [];

    /// <summary>离线成员（当前不在线，未推送来电，UI 可提示）</summary>
    public List<Guid> OfflineUsers { get; init; } = [];

    /// <summary>呼叫创建时间（UTC）</summary>
    public DateTimeOffset CreatedAt { get; init; }
}

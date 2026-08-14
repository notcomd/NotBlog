namespace Message.Web.API.Dto.Call;

/// <summary>
/// 通话信息（服务端 → 客户端的通话事件载体，如 <see cref="ICallClient.IncomingCall"/>）。
/// </summary>
public sealed class CallInfoDto
{
    /// <summary>通话 ID</summary>
    public Guid CallId { get; init; }

    /// <summary>发起通话的会话 ID（ChatSession）</summary>
    public Guid SessionId { get; init; }

    /// <summary>通话类型（语音 / 视频）</summary>
    public CallType Type { get; init; }

    /// <summary>当前通话状态</summary>
    public CallStatus Status { get; init; }

    /// <summary>呼叫方用户 ID</summary>
    public Guid CallerId { get; init; }

    /// <summary>会话全部参与者（含呼叫方；通话的合法成员集合）</summary>
    public List<Guid> Participants { get; init; } = [];

    /// <summary>已接通（进入通话）的成员</summary>
    public List<Guid> JoinedMembers { get; init; } = [];

    /// <summary>发起呼叫时检测到的忙线成员（无法接听来电）</summary>
    public List<Guid> BusyUsers { get; init; } = [];

    /// <summary>呼叫创建时间（UTC）</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>首个成员接通时间（UTC，响铃 → 通话中）</summary>
    public DateTimeOffset? ConnectedAt { get; init; }
}

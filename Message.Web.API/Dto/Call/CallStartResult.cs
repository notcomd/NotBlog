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

    /// <summary>呼叫方用户 ID</summary>
    public Guid CallerId { get; init; }

    /// <summary>会话全部参与者（含呼叫方）</summary>
    public List<Guid> Participants { get; init; } = [];

    /// <summary>忙线成员（无法接听来电，UI 可提示）</summary>
    public List<Guid> BusyUsers { get; init; } = [];

    /// <summary>离线成员（当前不在线，未推送来电，UI 可提示）</summary>
    public List<Guid> OfflineUsers { get; init; } = [];

    /// <summary>呼叫创建时间（UTC）</summary>
    public DateTimeOffset CreatedAt { get; init; }
}

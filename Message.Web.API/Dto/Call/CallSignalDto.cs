namespace Message.Web.API.Dto.Call;

/// <summary>
/// WebRTC 信令数据（服务端仅做转发，不解析媒体内容）。
/// <para>
/// 拓扑约定（Mesh 全网状）：
/// 通话建立或成员加入后，由「新加入成员」向通话内每个既有成员通过 <see cref="ICallClient.Signal"/>
/// 定向发送 <c>offer</c>；被邀方回 <c>answer</c>；双方随时互发 <c>ice</c>。
/// </para>
/// </summary>
public sealed class CallSignalDto
{
    /// <summary>所属通话 ID</summary>
    public Guid CallId { get; init; }

    /// <summary>发送方用户 ID（服务端填充，客户端无需携带）</summary>
    public Guid FromUserId { get; init; }

    /// <summary>目标用户 ID；为空表示广播给通话内其他已接通成员</summary>
    public Guid? ToUserId { get; init; }

    /// <summary>信令类型：<c>offer</c> / <c>answer</c> / <c>ice</c></summary>
    public string Kind { get; init; } = string.Empty;

    /// <summary>SDP 描述（offer / answer 时携带）</summary>
    public string? Sdp { get; init; }

    /// <summary>ICE candidate（ice 时携带）</summary>
    public string? Candidate { get; init; }

    /// <summary>ICE candidate 对应的 SDP mid（ice 时携带）</summary>
    public string? SdpMid { get; init; }

    /// <summary>ICE candidate 对应的 SDP m-line 索引（ice 时携带）</summary>
    public int? SdpMLineIndex { get; init; }
}

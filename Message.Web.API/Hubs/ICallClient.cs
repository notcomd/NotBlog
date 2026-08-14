using Message.Web.API.Dto.Call;

namespace Message.Web.API.Hubs;

/// <summary>
/// 通话 Hub（<see cref="CallHub"/>）客户端事件接口（服务端 → 客户端）。
/// <para>
/// 事件清单：
/// - <see cref="IncomingCall"/>：被叫方收到来电（响铃）；
/// - <see cref="CallStarted"/>：通话建立（至少一名被叫接通），此后进入 WebRTC 信令阶段；
/// - <see cref="CallEnded"/>：通话结束（含 reason：取消/拒绝/超时/全员离开/对方挂断/错误）；
/// - <see cref="MemberJoined"/>：成员接通（进入通话，既有成员据此与其建立对等连接）；
/// - <see cref="MemberLeft"/>：成员离开通话（群组通话）；
/// - <see cref="MemberRejected"/>：成员拒绝来电（通知呼叫方）；
/// - <see cref="Signal"/>：WebRTC 信令（offer/answer/ice）定向或广播转发。
/// </para>
/// </summary>
public interface ICallClient
{
    /// <summary>
    /// 被叫方收到来电。
    /// </summary>
    /// <param name="call">通话信息</param>
    Task IncomingCall(CallInfoDto call);

    /// <summary>
    /// 通话建立（响铃 → 通话中）。呼叫方与已接通成员均会收到。
    /// </summary>
    /// <param name="call">通话信息（含 JoinedMembers 成员快照）</param>
    Task CallStarted(CallInfoDto call);

    /// <summary>
    /// 通话结束。
    /// </summary>
    /// <param name="call">通话信息（终态）</param>
    /// <param name="reason">结束原因</param>
    Task CallEnded(CallInfoDto call, CallEndReason reason);

    /// <summary>
    /// 成员接通（进入通话）。通话内既有成员收到后，应与其建立 WebRTC 对等连接。
    /// </summary>
    /// <param name="call">通话信息（含最新 JoinedMembers 快照）</param>
    /// <param name="memberId">新接通成员</param>
    Task MemberJoined(CallInfoDto call, Guid memberId);

    /// <summary>
    /// 成员离开通话（群组通话中成员挂断/掉线）。
    /// </summary>
    /// <param name="call">通话信息</param>
    /// <param name="memberId">离开成员</param>
    Task MemberLeft(CallInfoDto call, Guid memberId);

    /// <summary>
    /// 成员拒绝来电（通知呼叫方）。
    /// </summary>
    /// <param name="call">通话信息</param>
    /// <param name="memberId">拒绝成员</param>
    Task MemberRejected(CallInfoDto call, Guid memberId);

    /// <summary>
    /// WebRTC 信令转发（offer / answer / ice）。
    /// </summary>
    /// <param name="signal">信令数据（FromUserId 由服务端填充）</param>
    Task Signal(CallSignalDto signal);
}

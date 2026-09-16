namespace Message.Web.API.Dto.Call;

/// <summary>
/// 通话结束原因（<see cref="ICallClient.CallEnded"/> 的 reason 参数）。
/// </summary>
public enum CallEndReason
{
    /// <summary>
    /// 呼叫方取消（响铃阶段主动挂断）
    /// </summary>
    CallerCancelled = 0,

    /// <summary>
    /// 被叫拒绝（1 对 1 通话被拒后通话结束）
    /// </summary>
    Rejected = 1,

    /// <summary>
    /// 超时无人应答（服务端兜底）
    /// </summary>
    Timeout = 2,

    /// <summary>
    /// 全部成员离开（群组通话）
    /// </summary>
    AllLeft = 3,

    /// <summary>
    /// 对方挂断（1 对 1 通话远端挂断）
    /// </summary>
    RemoteHangup = 4,

    /// <summary>
    /// 服务端错误
    /// </summary>
    Error = 5,

    /// <summary>
    /// 房间被创建者关闭（常驻房间）
    /// </summary>
    RoomClosed = 6
}

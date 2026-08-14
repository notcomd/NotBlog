namespace Message.Web.API.Dto.Call;

/// <summary>
/// 通话状态。
/// </summary>
public enum CallStatus
{
    /// <summary>
    /// 响铃中：呼叫已发起，等待被叫应答
    /// </summary>
    Ringing = 0,

    /// <summary>
    /// 通话中：至少一名被叫已接通
    /// </summary>
    Active = 1,

    /// <summary>
    /// 已结束（终态）
    /// </summary>
    Ended = 2
}

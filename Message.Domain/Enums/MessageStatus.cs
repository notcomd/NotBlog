namespace Message.Domain.Enums;

public enum MessageStatus
{
    /// <summary>
    /// 待发送状态（状态机预留：发送链路当前直接置 Sent）
    /// </summary>
    Pending,

    /// <summary>
    /// 已发送状态
    /// </summary>
    Sent,

    /// <summary>
    /// 已送达状态（状态机预留：当前未在发送链路使用，可留待 SignalR 送达回执接入）
    /// </summary>
    Delivered,

    /// <summary>
    /// 已读状态
    /// </summary>
    Read,

    /// <summary>
    /// 已撤回状态
    /// </summary>
    Recalled,

    /// <summary>
    /// 失败状态（状态机预留：当前发送失败直接抛异常，不落 Failed 状态）
    /// </summary>
    Failed
}
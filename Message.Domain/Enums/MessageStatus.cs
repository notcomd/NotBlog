namespace Message.Domain.Enums;

public enum MessageStatus
{
    /// <summary>
    /// 待发送状态
    /// </summary>
    Pending,

    /// <summary>
    /// 已发送状态
    /// </summary>
    Sent,

    /// <summary>
    /// 已送达状态
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
    /// 失败状态
    /// </summary>
    Failed
}
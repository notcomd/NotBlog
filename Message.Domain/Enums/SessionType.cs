namespace Message.Domain.Enums;

public enum SessionType
{
    /// <summary>
    /// AI类型
    /// </summary>
    AiChat,

    /// <summary>
    /// 私聊会话
    /// </summary>
    Private,

    /// <summary>
    /// 群聊会话
    /// </summary>
    Group,

    // 频道会话
    Channel,

    ///匿名会话
    NonAnonymous,
}
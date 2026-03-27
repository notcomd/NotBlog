namespace Message.Domain.Enums;

public enum MessageType
{
    /// <summary>
    /// 文本消息
    /// </summary>
    MessageText,

    /// <summary>
    /// 图片消息
    /// </summary>
    MessageImage,

    /// <summary>
    /// 视频消息
    /// </summary>
    MessageVideo,

    /// <summary>
    /// 音频消息
    /// </summary>
    MessageAudio,

    /// <summary>
    /// 文件消息
    /// </summary>
    MessageFile,

    /// <summary>
    /// 推送消息
    /// </summary>
    MessagePush,

    /// <summary>
    /// 位置消息
    /// </summary>
    MessageLocation,

    /// <summary>
    /// 链接消息
    /// </summary>
    MessageLink,

    /// <summary>
    /// 面包消息
    /// </summary>
    MessageExpression,

    /// <summary>
    /// 其他消息
    /// </summary>
    MessageOther
}
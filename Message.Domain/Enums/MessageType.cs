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
    /// 推送消息（预留：系统公告等场景；发送链路暂不支持，SendMessageCommandHandler 抛 NotSupportedException）
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
    /// 其他消息（预留：无明确业务场景，发送链路暂不支持）
    /// </summary>
    MessageOther
}
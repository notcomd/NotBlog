using Message.Domain.Enums;

namespace Message.Domain.Entities.Recall;

public class RecallConfig
{
    public int PrivateChatRecallMinutes { get; set; } = 2;
    public int GroupChatRecallMinutes { get; set; } = 2;
    public bool AllowRecallForAllMessageTypes { get; set; } = true;

    public HashSet<MessageType> RecallableMessageTypes { get; set; } = new()
    {
        MessageType.MessageText,
        MessageType.MessageImage,
        MessageType.MessageVideo,
        MessageType.MessageAudio,
        MessageType.MessageFile,
        MessageType.MessageLink,
        MessageType.MessageExpression
    };

    public bool CanRecallMessageType(MessageType messageType)
    {
        if (AllowRecallForAllMessageTypes)
            return true;
        return RecallableMessageTypes.Contains(messageType);
    }

    /// <summary>
    /// 获取消息撤回时间限制
    /// </summary>
    /// <param name="sessionType"></param>
    /// <returns></returns>
    public int GetRecallMinutes(SessionType sessionType)
    {
        return sessionType switch
        {
            SessionType.Private => PrivateChatRecallMinutes,
            SessionType.Group => GroupChatRecallMinutes,
            _ => 2
        };
    }
}
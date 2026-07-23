using Message.Domain.Enums;

namespace Message.Domain.Entities.Recall;

public class RecallConfig
{
    public int PrivateChatRecallMinutes { get; private set; }
    public int GroupChatRecallMinutes { get; private set; }
    public bool AllowRecallForAllMessageTypes { get; private set; }

    public HashSet<MessageType> RecallableMessageTypes { get; private set; } = new()
    {
        MessageType.MessageText,
        MessageType.MessageImage,
        MessageType.MessageVideo,
        MessageType.MessageAudio,
        MessageType.MessageFile,
        MessageType.MessageLink,
        MessageType.MessageExpression
    };

    private RecallConfig()
    {
        PrivateChatRecallMinutes = 2;
        GroupChatRecallMinutes = 2;
        AllowRecallForAllMessageTypes = true;
    }

    public static RecallConfig Create(int privateChatRecallMinutes = 2, int groupChatRecallMinutes = 2,
        bool allowRecallForAllMessageTypes = true, HashSet<MessageType>? recallableMessageTypes = null)
    {
        var config = new RecallConfig
        {
            PrivateChatRecallMinutes = privateChatRecallMinutes,
            GroupChatRecallMinutes = groupChatRecallMinutes,
            AllowRecallForAllMessageTypes = allowRecallForAllMessageTypes
        };
        if (recallableMessageTypes is not null)
            config.RecallableMessageTypes = recallableMessageTypes;
        return config;
    }

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
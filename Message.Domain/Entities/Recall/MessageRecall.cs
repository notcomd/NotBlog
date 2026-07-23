using Message.Domain.Enums;
using Message.Domain.SeedWork;

namespace Message.Domain.Entities.Recall;

/// <summary>
///  消息撤回
/// </summary>
public class MessageRecall : Entity
{
    public MessageRecall(Guid messageId, Guid recalledBy, RecallReason reason, string? originalContent,
        int timeLimitMinutes = 2)
    {
        Id = Guid.CreateVersion7();
        MessageId = messageId;
        RecalledBy = recalledBy;
        RecallTime = DateTime.UtcNow;
        Reason = reason;
        OriginalContent = originalContent;
        IsWithinTimeLimit = true;
    }

    private MessageRecall()
    {
        Id = Guid.CreateVersion7();
        RecallTime = DateTime.UtcNow;
    }

    public Guid MessageId { get; init; }

    public Guid RecalledBy { get; init; }

    public DateTime RecallTime { get; init; }

    public RecallReason Reason { get; private set; }

    public string? OriginalContent { get; init; }

    public bool IsWithinTimeLimit { get; private set; }


    public static bool CanRecall(DateTime messageSentTime, int timeLimitMinutes = 2)
    {
        var timeDiff = DateTime.UtcNow - messageSentTime;
        return timeDiff.TotalMinutes <= timeLimitMinutes;
    }

    public void MarkAsExpired()
    {
        IsWithinTimeLimit = false;
    }
}
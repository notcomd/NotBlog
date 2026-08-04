using Message.Domain.Enums;
using Commons.SeedWork;

namespace Message.Domain.Entities.Forward;

/// <summary>
/// 消息转发实体
/// </summary>
public class MessageForward : Entity<Guid>
{
    public MessageForward(Guid originalMessageId, Guid forwardedMessageId, Guid forwardedBy,
        Guid targetSessionId, ForwardType forwardType, Guid? parentForwardId = null, string? comment = null) : this()
    {
        OriginalMessageId = originalMessageId;
        ForwardedMessageId = forwardedMessageId;
        ForwardedBy = forwardedBy;
        TargetSessionId = targetSessionId;
        ForwardType = forwardType;
        ForwardTime = DateTime.UtcNow;
        ForwardComment = comment;
        ParentForwardId = parentForwardId;
        ForwardDepth = 1;
        ForwardChain.Add(Id);
    }

    private MessageForward()
    {
        Id = Guid.CreateVersion7();
        ForwardTime = DateTime.UtcNow;
        ForwardChain = new List<Guid>();
    }

    public Guid OriginalMessageId { get; init; }

    public Guid ForwardedMessageId { get; init; }

    public Guid ForwardedBy { get; init; }

    public Guid TargetSessionId { get; init; }

    public ForwardType ForwardType { get; private set; }

    public DateTime ForwardTime { get; init; }

    public string? ForwardComment { get; private set; }

    public Guid? ParentForwardId { get; init; }

    public int ForwardDepth { get; private set; }

    public List<Guid> ForwardChain { get; init; }


    public static MessageForward CreateNestedForward(MessageForward parentForward, Guid newForwardedMessageId,
        Guid forwardedBy, Guid targetSessionId, ForwardType forwardType, string? comment = null)
    {
        if (parentForward == null)
            throw new ArgumentNullException(nameof(parentForward));

        var forward = new MessageForward
        {
            OriginalMessageId = parentForward.OriginalMessageId,
            ForwardedMessageId = newForwardedMessageId,
            ForwardedBy = forwardedBy,
            TargetSessionId = targetSessionId,
            ForwardType = forwardType,
            ForwardTime = DateTime.UtcNow,
            ForwardComment = comment,
            ParentForwardId = parentForward.Id,
            ForwardDepth = parentForward.ForwardDepth + 1,
            ForwardChain = new List<Guid>(parentForward.ForwardChain)
        };
        forward.ForwardChain.Add(forward.Id);
        return forward;
    }

    public bool IsMultipleForward()
    {
        return ForwardDepth > 1;
    }

    public string GetForwardPath()
    {
        return string.Join(" -> ", ForwardChain);
    }
}
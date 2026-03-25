using Message.Domain.Enums;
using NotMediator;

namespace Message.Domain.Events;

public record MessageSentEvent(Guid MessageId, Guid SenderId, Guid? ReceiverId, Guid SessionId, MessageType MessageType)
    : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record MessageReceivedEvent(Guid MessageId, Guid ReceiverId, DateTime ReceivedTime) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record MessageReadEvent(Guid MessageId, Guid ReaderId, DateTime ReadTime) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record MessageRecalledEvent(Guid MessageId, Guid RecalledBy, RecallReason Reason) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record MessageForwardedEvent(
    Guid OriginalMessageId,
    Guid ForwardedMessageId,
    Guid ForwardedBy,
    Guid TargetSessionId) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record FileUploadedEvent(Guid AttachmentId, Guid MessageId, string FileName, long FileSize) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record FileDownloadedEvent(Guid AttachmentId, Guid UserId, DateTime DownloadTime) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record UserOnlineEvent(Guid UserId, DateTime OnlineTime) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record UserOfflineEvent(Guid UserId, DateTime OfflineTime) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record FriendshipCreatedEvent(Guid UserId, Guid FriendId) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record FriendshipAcceptedEvent(Guid UserId, Guid FriendId) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record SessionCreatedEvent(Guid SessionId, HashSet<Guid> Participants, SessionType SessionType) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record GroupCreatedEvent(Guid GroupId, Guid OwnerId, string GroupName) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record GroupMemberJoinedEvent(Guid GroupId, Guid UserId, GroupMemberRole Role) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record GroupMemberLeftEvent(Guid GroupId, Guid UserId) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
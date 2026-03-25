using NotMediator;

namespace Message.Domain.Events;

public record FileDownloadedEvent(
    Guid AttachmentId,
    Guid UserId,
    DateTime DownloadTime) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
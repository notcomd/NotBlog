
namespace Message.Domain.Events;

public record FileUploadedEvent(
    Guid AttachmentId,
    Guid MessageId,
    string FileName,
    long FileSize) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
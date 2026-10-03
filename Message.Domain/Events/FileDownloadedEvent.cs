
namespace Message.Domain.Events;

/// <summary>文件下载事件。</summary>
public record FileDownloadedEvent(
    Guid AttachmentId,
    Guid UserId,
    DateTime DownloadTime) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
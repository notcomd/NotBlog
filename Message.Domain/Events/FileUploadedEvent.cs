
namespace Message.Domain.Events;

/// <summary>文件上传事件。</summary>
public record FileUploadedEvent(
    Guid AttachmentId,
    Guid MessageId,
    string FileName,
    long FileSize) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
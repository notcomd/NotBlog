using NotMediator;

namespace FileDev.Domain.Events;

public class FileUploadedEvent(Guid fileId, Guid userId, string fileName):INotifications
{
    public Guid FileId { get; } = fileId;
    public Guid UserId { get; } = userId;
    public string FileName { get; } = fileName;
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
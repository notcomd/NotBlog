using NotMediator;

namespace FileDev.Domain.Events;

public class FileDeletedEvent(Guid fileId, Guid userId):INotifications
{
    public Guid FileId { get; } = fileId;
    public Guid UserId { get; } = userId;
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
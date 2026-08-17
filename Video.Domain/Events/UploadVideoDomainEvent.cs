
namespace Video.Domain.Events;

public record UploadVideoDomainEvent(Guid VideoId, string VideoName, string VideoUrl):INotifications;

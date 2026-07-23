using NotMediator;
namespace Video.Domain.Events;

public record PushVideoViewDomainEvent(Guid ReviewGuid, Guid VideoGuid, string Content, string UserId):INotifications;

namespace Identity.Domain.Events;

public record AccountLockedEvent(Guid UserGuid) : INotifications;
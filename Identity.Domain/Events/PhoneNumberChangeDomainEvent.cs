namespace Identity.Domain.Events;

public record PhoneNumberChangeDomainEvent(Guid UserGuid, string PhoneNumber) : INotifications;
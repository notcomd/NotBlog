namespace Identity.Domain.Events;

public record PhoneNumberBandingEvent(Guid UserGuid, string PhoneNumber) : INotifications;
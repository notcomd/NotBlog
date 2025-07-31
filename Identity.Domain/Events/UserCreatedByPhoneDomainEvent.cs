namespace Identity.Domain.Events;

public record UserCreatedByPhoneDomainEvent(User UserTrcInfo, PhoneNumber PhoneNumber, string UserName, DateTimeOffset DateTimeOffset) : INotifications;
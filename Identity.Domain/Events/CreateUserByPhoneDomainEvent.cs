namespace Identity.Domain.Events;

public record CreateUserByPhoneDomainEvent(User UserTrcInfo, PhoneNumber PhoneNumber, string UserName, DateTimeOffset DateTimeOffset) : INotifications;
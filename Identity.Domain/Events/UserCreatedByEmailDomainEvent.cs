using System.Security;

namespace Identity.Domain.Events;

public record UserCreatedByEmailDomainEvent(User UserTrcInfo, string Email, string UserName, DateTimeOffset CreatedTime) : INotifications;
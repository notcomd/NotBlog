using System.Security;

namespace Identity.Domain.Events;

public record CreateUserByEmailDomainEvent(User UserTrcInfo, string Email, string UserName, DateTimeOffset CreatedTime) : INotifications;
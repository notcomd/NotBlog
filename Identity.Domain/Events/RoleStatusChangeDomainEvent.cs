using Identity.Domain.Entities.RoleAggregate;

namespace Identity.Domain.Events;

public record RoleStatusChangeDomainEvent(
    Guid RoleGuid,
    RoleStatus RoleStatus
) : INotifications;
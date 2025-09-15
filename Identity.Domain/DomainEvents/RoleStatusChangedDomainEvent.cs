using Identity.Domain.AggregatesModel.RoleAggregate;

namespace Identity.Domain.DomainEvents;

public class RoleStatusChangedDomainEvent : INotifications
{
    public Guid RoleId { get; }
    public EnRoleStatus RoleStatus { get; }

    public RoleStatusChangedDomainEvent(Guid roleId, EnRoleStatus roleStatus)
    {
        RoleId = roleId;
        RoleStatus = roleStatus;
    }
}
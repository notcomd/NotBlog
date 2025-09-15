using Identity.Domain.AggregatesModel.RoleAggregate;

namespace Identity.Domain.DomainEvents;

public class RoleAuthorityChangedDomainEvent : INotifications
{
    public Guid RoleId { get; }
    public EnRoleAuthority RoleAuthority { get; }

    public RoleAuthorityChangedDomainEvent(Guid roleId, EnRoleAuthority roleAuthority)
    {
        RoleId = roleId;
        RoleAuthority = roleAuthority;
    }
}
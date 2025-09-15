using Identity.Domain.AggregatesModel.RoleAggregate;

namespace Identity.Domain.DomainEvents;

public class RoleClaimsChangedDomainEvent : INotifications
{
    public Guid RoleId { get; }
    public IEnumerable<RoleClaim> RoleClaims { get; }

    public RoleClaimsChangedDomainEvent(Guid roleId, IEnumerable<RoleClaim> roleClaims)
    {
        RoleId = roleId;
        RoleClaims = roleClaims;
    }
}
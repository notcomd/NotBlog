namespace Identity.Domain.DomainEvents;

public class RoleClaimAddedDomainEvent : INotifications
{
    public Guid RoleId { get; }
    public string ClaimType { get; }
    public string ClaimValue { get; }

    public RoleClaimAddedDomainEvent(Guid roleId, string claimType, string claimValue)
    {
        RoleId = roleId;
        ClaimType = claimType;
        ClaimValue = claimValue;
    }
}
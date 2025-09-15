namespace Identity.Domain.DomainEvents;

public class RoleNameChangedDomainEvent : INotifications
{
    public Guid RoleId { get; }
    public string NewRoleName { get; }

    public RoleNameChangedDomainEvent(Guid roleId, string newRoleName)
    {
        RoleId = roleId;
        NewRoleName = newRoleName;
    }
}
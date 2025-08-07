namespace Identity.Domain.Events;

public record CreatedByRoleDomainEvent : INotifications
{

    public CreatedByRoleDomainEvent(Guid roleGuid, string roleName, string roleAttribute)
    {
        RoleGuid = roleGuid;
        RoleName = roleName;
        RoleAttribute = roleAttribute;
    }

    public Guid? RoleGuid { get; }

    public string RoleName { get; }

    public string RoleAttribute { get; set; }

   
}

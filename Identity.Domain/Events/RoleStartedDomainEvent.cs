namespace Identity.Domain.Events
{
    public record RoleStartedDomainEvent(
        Roles Roles
    ) : INotifications;

}
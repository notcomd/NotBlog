namespace Identity.Domain.Events
{

    public record AccountLockedDomainEvent(Guid UserGuid) : INotifications;

}
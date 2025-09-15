namespace Identity.Domain.DomainEvents
{

    public record AccountLockedDomainEvent(Guid UserGuid,DateTimeOffset? DateTimeOffset) : INotifications;

}
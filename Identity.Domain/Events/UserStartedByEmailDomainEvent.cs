namespace Identity.Domain.Events
{
    public record UserStartedByEmailDomainEvent(HashSet<Guid> userRoleGuid, string userEmail, string passwordHash, Uri imageCover) : INotifications;

}
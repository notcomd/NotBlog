namespace Identity.Domain.Events
{
    public record UserStartedByEmailDomainEvent(Guid userRoleGuid, string userEmail, string passwordHash, Uri imageCover) : INotifications;

}
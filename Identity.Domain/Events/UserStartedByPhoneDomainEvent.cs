namespace Identity.Domain.Events
{
    public record UserStartedByPhoneDomainEvent(HashSet<Guid> userRoleGuid, PhoneNumber phoneNumber, string passwordHash, Uri imageCover) : INotifications;

}
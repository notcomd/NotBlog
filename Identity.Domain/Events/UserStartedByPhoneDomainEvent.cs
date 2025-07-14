namespace Identity.Domain.Events
{
    public record UserStartedByPhoneDomainEvent(Guid userRoleGuid, PhoneNumber phoneNumber, string passwordHash, Uri imageCover) : INotifications;

}
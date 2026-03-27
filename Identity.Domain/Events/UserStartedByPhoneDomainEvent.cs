namespace Identity.Domain.Events;

public record UserStartedByPhoneDomainEvent(
    HashSet<Guid> UserRoleGuid,
    PhoneNumber PhoneNumber,
    string PasswordHash,
    Uri ImageCover,
    HashSet<Guid> AuthorGuid) : INotifications;
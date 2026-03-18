namespace Identity.Domain.Events;

public record UserStartedByEmailDomainEvent(
    HashSet<Guid> UserRoleGuid,
    string UserEmail,
    string PasswordHash,
    Uri ImageCover,HashSet<Guid>? RoleGuid) : INotifications;
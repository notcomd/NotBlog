using System.Security;

namespace Identity.Domain.DomainEvents;

public record CreatedByUserDomainEvent : INotifications
{

    public CreatedByUserDomainEvent(Guid userGuid, Guid roleGuid, string name, string? email,
        PhoneNumber? phoneNumber, DateTimeOffset dateTimeOffset)
    {
        UserGuid = userGuid;
        RoleGuid = roleGuid;
        Name = name;
        Email = email;
        PhoneNumber = phoneNumber;
        DateTimeOffset = dateTimeOffset;
    }

    public Guid UserGuid { get; set; }

    public Guid RoleGuid { get; set; }

    public string Name { get; set; }

    public string? Email { get; set; }

    public PhoneNumber? PhoneNumber { get; set; }

    public DateTimeOffset DateTimeOffset { get; set; }
}
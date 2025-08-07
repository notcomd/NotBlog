namespace Identity.Domain.Events;

public record ChangeByUserStatusDomianEvent : INotifications
{

    public ChangeByUserStatusDomianEvent(Guid userGuid, string eventMessage, string userName, string? email, PhoneNumber? phoneNumber, UserSafety? userSafety, UserAccessFail? userAccessFail, DateTimeOffset dateTimeOffset)
    {
        UserGuid = userGuid;

        UserName = userName;
        Email = email;
        PhoneNumber = phoneNumber;
        UserSafety = userSafety;
        UserAccessFail = userAccessFail;
        DateTimeOffset = dateTimeOffset;
        EventMessage = eventMessage;
    }

    public Guid UserGuid { get; }

    public string UserName { get; set; } = string.Empty;

    public string? Email { get; set; } = string.Empty;

    public PhoneNumber? PhoneNumber { get; set; }

    public UserSafety? UserSafety { get; set; }

    public UserAccessFail? UserAccessFail { get; set; }

    public string EventMessage { get; set; }

    public DateTimeOffset DateTimeOffset { get; }


}
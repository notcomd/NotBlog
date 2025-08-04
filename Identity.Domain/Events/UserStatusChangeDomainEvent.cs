namespace Identity.Domain.Events;

public class UserStatusChangeDomainEvent : INotifications
{

    public UserStatusChangeDomainEvent(Guid userGuid,string userName,string email, EnumUserStatus userStatus, PhoneNumber phoneNumber,DateTimeOffset dateTimeOffset)
    {
        UserGuid = userGuid;
        UserName = userName ?? throw new ArgumentNullException(nameof(userName), "UserName cannot be null or empty");
        Email = email ?? throw new ArgumentNullException(nameof(email), "Email cannot be null or empty");
        UserStatus = userStatus;
        PhoneNumber = phoneNumber;
        DateTimeOffset = dateTimeOffset;
    }

    public Guid UserGuid { get; }

    public string UserName { get; set; } = string.Empty;

    public  string Email { get; set; } = string.Empty;

    public PhoneNumber PhoneNumber { get; set; }

    public EnumUserStatus UserStatus { get; }

    public DateTimeOffset DateTimeOffset { get; }


}
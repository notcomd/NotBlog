
namespace Identity.Domain.AggregatesModel.UserAggregate;

/// <summary>
/// 登入事件
/// </summary>
public class UserLoginHistory : Entity, IAggregateRoot
{


    private UserLoginHistory()
    {

    }



    public UserLoginHistory CreateByUserLoginHistoryAsync(Guid userId, PhoneNumber? phoneNumber, string? email, string loginMessage, DateTimeOffset dateTimeOffset)
    {
        return new UserLoginHistory
        {
            Id = Guid.CreateVersion7(),
            UserGuid = userId != Guid.Empty ? userId : throw new ArgumentNullException(nameof(userId), "UserGuid cannot be empty"),
            PhoneNumber = phoneNumber,
            Email = email,
            CreateDataTime = dateTimeOffset,
            LoginMessage = loginMessage
        };

    }



    public Guid UserGuid { get; init; }

    public PhoneNumber? PhoneNumber { get; private set; }

    [EmailAddress(ErrorMessage = "Error Email Address!")]
    public string? Email { get; private set; }

    public DateTimeOffset CreateDataTime { get; private set; }

    public string? LoginMessage { get; private set; }


    public void SetOrResetPhoneNumber(PhoneNumber phoneNumber)
    {
        ArgumentNullException.ThrowIfNull(phoneNumber);
        PhoneNumber = phoneNumber;
    }

    public void SetOrResetEmail(string email)
    {
        ArgumentNullException.ThrowIfNull(email);
        if (!new EmailAddressAttribute().IsValid(email))
            throw new ArgumentException("Invalid email format", nameof(email));
        Email = email;
    }


    public void SetOrResetLoginMessage(string loginMessage)
    {
        ArgumentNullException.ThrowIfNull(loginMessage);
        LoginMessage = loginMessage;
    }


    public void SetOrResetCreateDataTime(DateTimeOffset createDataTime)
    {
        if (createDataTime == default)
            throw new ArgumentException("CreateDataTime cannot be default value", nameof(createDataTime));
        CreateDataTime = createDataTime;
    }



}
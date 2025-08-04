
namespace Identity.Domain.AggregatesModel.UserAggregate;

public class UserLoginHistory
{


    private UserLoginHistory()
    {
    }

   

    public ValueTask<UserLoginHistory> CreateByUserLoginHistoryAsync(Guid userId, PhoneNumber? phoneNumber, string loginMessage, string? email, DateTimeOffset dateTimeOffset)
    {
        ArgumentNullException.ThrowIfNull(phoneNumber);
        ArgumentNullException.ThrowIfNull(loginMessage);
        ArgumentNullException.ThrowIfNull(email);
        var userLoginHistory = new UserLoginHistory
        {
            LoginGuid = Guid.CreateVersion7(),
            UserGuid = userId != Guid.Empty ? userId : throw new ArgumentNullException(nameof(userId), "UserGuid cannot be empty"),
            PhoneNumber = phoneNumber,
            Email = email,
            CreateDataTime = dateTimeOffset,
            LoginMessage = loginMessage
        };
        return new ValueTask<UserLoginHistory>(userLoginHistory);
    }


    public Guid LoginGuid { get; init; }

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

    public override bool Equals(object? obj)
    {
        return obj is UserLoginHistory history &&
               LoginGuid.Equals(history.LoginGuid) &&
               UserGuid.Equals(history.UserGuid) &&
               EqualityComparer<PhoneNumber?>.Default.Equals(PhoneNumber, history.PhoneNumber) &&
               Email == history.Email &&
               CreateDataTime.Equals(history.CreateDataTime) &&
               LoginMessage == history.LoginMessage;
    }

}
namespace Identity.Domain.Entities;

public class UserLoginHistory
{
    
    public Guid LoginGuid { get; init; }
    public Guid UserGuid { get; init; }
    public PhoneNumber PhoneNumber { get; init; }
    public DateTime CreateDataTime { get; init; }
    public string? LoginMessage { get; set; }
    
    private UserLoginHistory(){}

    public UserLoginHistory(Guid userId, PhoneNumber phoneNumber, string loginMessage)
    {
        LoginGuid = new Guid();
        UserGuid = userId;
        PhoneNumber = phoneNumber;
        CreateDataTime = DateTime.UtcNow;
        LoginMessage = loginMessage;
    }
    
    
    
}
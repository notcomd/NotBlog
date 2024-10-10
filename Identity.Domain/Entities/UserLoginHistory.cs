using System.ComponentModel.DataAnnotations;

namespace Identity.Domain.Entities;

public class UserLoginHistory
{
    private UserLoginHistory()
    {
    }

    public UserLoginHistory(Guid userId, PhoneNumber phoneNumber, string loginMessage, string email)
    {
        LoginGuid = new Guid();
        UserGuid = userId;
        Email = email;
        PhoneNumber = phoneNumber;
        CreateDataTime = DateTime.Now;
        LoginMessage = loginMessage;
    }

    public Guid LoginGuid { get; init; }

    public Guid UserGuid { get; init; }

    public PhoneNumber PhoneNumber { get; init; }

    [EmailAddress(ErrorMessage = "Error Email Address!")]

    public string? Email { get; set; }

    public DateTime CreateDataTime { get; init; }

    public string? LoginMessage { get; set; }
}
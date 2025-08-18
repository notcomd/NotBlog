namespace Identity.Web.API.Application.Command;

public class ChangeByUserCommand : IRequest<bool>
{
    public string Email { get; init; }
    public PhoneNumber? PhoneNumber { get; set; }

    public string UserName { get; set; }

    public string 

    public UserSafety UserSafety { get; set; }
    
    public List<UserClaim> UserClaim { get; set; } = new List<UserClaim>();
    public UserAccessFail UserAccessFail { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public ChangeByUserCommand(string email, PhoneNumber? phoneNumber, UserSafety userSafety, UserAccessFail userAccessFail)
    {
        Email = email ?? throw new ArgumentNullException(nameof(email));
        UserSafety = userSafety ?? throw new ArgumentNullException(nameof(userSafety));
        UserAccessFail = userAccessFail ?? throw new ArgumentNullException(nameof(userAccessFail));
    }
    public void AddByUserClaims(UserClaim userClaim)
    {
        ArgumentNullException.ThrowIfNull(userClaim);
        UserClaim.Add(userClaim);
    }
}

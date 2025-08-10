

namespace Identity.Web.API.Application.Command;

public record CreateByEmailUserCommand : IRequest<bool>
{

    public string Email { get; set; } 

    public string Password { get; set; }

    public string RoleName { get; set; }

    public string Attribute { get; set; }

    public UserSafety UserSafety { get; set; }

    public List<UserClaim> UserClaim { get; set; }= new List<UserClaim>();

    public UserAccessFail UserAccessFail { get; set; }

    public DateTimeOffset CreatedAt { get; init; }


    public CreateByEmailUserCommand(string email, string password, string roleName, string attribute, UserSafety userSafety, UserAccessFail userAccessFail,DateTimeOffset dateTimeOffset)
    {
        Email = email ?? throw new ArgumentNullException(nameof(email));
        Password = password ?? throw new ArgumentNullException(nameof(password));
        RoleName = roleName ?? throw new ArgumentNullException(nameof(roleName));
        Attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));
        UserSafety = userSafety ?? throw new ArgumentNullException(nameof(userSafety));
        UserAccessFail = userAccessFail ?? throw new ArgumentNullException(nameof(userAccessFail));
        CreatedAt = dateTimeOffset == default ? DateTimeOffset.UtcNow : dateTimeOffset;
    }


    public void AddByUserClaims(UserClaim userClaim)
    {
        ArgumentNullException.ThrowIfNull(userClaim);
        UserClaim.Add(userClaim);
    }
}
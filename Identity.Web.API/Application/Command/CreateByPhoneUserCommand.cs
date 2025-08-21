namespace Identity.Web.API.Application.Command;

public sealed record CreateByPhoneUserCommand : IRequest<bool>
{
    public PhoneNumber PhoneNumber { get; set; }

    public string Password { get; set; }

    public string RoleName { get; set; }

    public string Attribute { get; set; }
    
    public DateTimeOffset CreatedAt { get; init; }


    public CreateByPhoneUserCommand(PhoneNumber phoneNumber, string password, string roleName, 
        string attribute, DateTimeOffset dateTimeOffset)
    {
        PhoneNumber = phoneNumber ?? throw new ArgumentNullException(nameof(phoneNumber));
        Password = password ?? throw new ArgumentNullException(nameof(password));
        RoleName = roleName ?? throw new ArgumentNullException(nameof(roleName));
        Attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));        
        CreatedAt = dateTimeOffset == default ? DateTimeOffset.UtcNow : dateTimeOffset;
    }
}


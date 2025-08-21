

namespace Identity.Web.API.Application.Command;

public record CreateByEmailUserCommand : IRequest<bool>
{

    public string Email { get; set; }

    public string Password { get; set; }

    public string RoleName { get; set; }

    public string Attribute { get; set; }

    public DateTimeOffset CreatedAt { get; init; }


    public CreateByEmailUserCommand(string email, string password,
        string roleName, string attribute, DateTimeOffset dateTimeOffset)
    {
        Email = email ?? throw new ArgumentNullException(nameof(email));
        Password = password ?? throw new ArgumentNullException(nameof(password));
        RoleName = roleName ?? throw new ArgumentNullException(nameof(roleName));
        Attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));

        CreatedAt = dateTimeOffset == default ? DateTimeOffset.UtcNow : dateTimeOffset;
    }
}
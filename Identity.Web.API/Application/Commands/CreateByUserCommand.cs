namespace Identity.Web.API.Application.Commands;

public class CreateUserCommand : IRequest<bool>
{
    public CreateUserCommand(string email, string password, string code)
    {
        Email = email;
        Password = password;
        Code = code;
    }

    public string Email { get; set; }

    public string Password { get; set; }

    public string Code { get; set; }
}
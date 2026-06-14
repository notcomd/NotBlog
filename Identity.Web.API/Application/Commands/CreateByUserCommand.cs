namespace Identity.Web.API.Application.Commands;

public class CreateUserCommand(string email, string password, string code) : IRequest<bool>
{
    public string Email { get; set; } = email;

    public string Password { get; set; } = password;

    public string Code { get; set; } = code;
}
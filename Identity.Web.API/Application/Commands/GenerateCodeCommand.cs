namespace Identity.Web.API.Application.Commands;

public record GenerateCodeCommand(string Email) : IRequest<string>, ILoggableCommand
{
    public string IdProperty => nameof(Email);
    public string IdValue => Email;
}
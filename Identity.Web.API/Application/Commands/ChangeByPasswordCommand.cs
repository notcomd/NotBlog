namespace Identity.Web.API.Application.Commands;

public record ChangeByPasswordCommand(string Email, string NewPasswordHash) : IRequest<bool>, ILoggableCommand
{
    public string IdProperty => nameof(Email);
    public string IdValue => Email;
}
namespace Identity.Web.API.Application.Commands;

public record ChangeByPasswordCommand(Guid UserId, string NewPassword) : IRequest<bool>, ILoggableCommand
{
    public string IdProperty => nameof(UserId);
    public string IdValue => UserId.ToString();
}
namespace Identity.Web.API.Application.Commands;

public record ChangeByPasswordCommand(string Email, string NewPasswordHash) : IRequest<bool>;
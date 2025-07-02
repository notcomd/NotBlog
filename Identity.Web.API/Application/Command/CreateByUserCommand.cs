namespace Identity.Web.API.Application.Command;

public record CreateByUserCommand(string Email, string Password, string Code) : IRequest<bool>;
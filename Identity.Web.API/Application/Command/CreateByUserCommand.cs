namespace Identity.Web.API.Application.Command;

public record CreateByUserCommand(string Email, string Password, string RoleName,string Attribute) : IRequest<bool>;
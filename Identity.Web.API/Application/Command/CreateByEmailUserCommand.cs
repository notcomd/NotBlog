namespace Identity.Web.API.Application.Command;

public record CreateByEmailUserCommand(string Email, string Password, string RoleName,string Attribute) : IRequest<bool>;
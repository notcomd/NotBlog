namespace Identity.Web.API.Application.Commands;

public record CreateByRoleCommand(string RoleName, string RoleDescription) : IRequest<bool>;
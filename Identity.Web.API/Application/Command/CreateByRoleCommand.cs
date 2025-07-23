namespace Identity.Web.API.Application.Command;

public sealed record CreateByRoleCommand(string RoleName, string Attribute) : IRequest<bool>
{
}

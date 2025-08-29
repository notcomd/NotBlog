namespace Identity.Web.API.Application.Command;

public sealed record CreateByRoleCommand : IRequest<bool>
{

    public string RoleName { get;}

    public string? Attribute { get;}

    public EnRoleAuthority? RoleAuthority { get; }

    public EnRoleStatus? RoleStatus { get; }




    public CreateByRoleCommand(string roleName, string? attribute, EnRoleAuthority roleAuthority, EnRoleStatus roleStatus)
    {
        RoleName = roleName ?? throw new ArgumentNullException(nameof(roleName));
        Attribute = attribute;
        RoleAuthority = roleAuthority;
        RoleStatus = roleStatus;        
    }  


}


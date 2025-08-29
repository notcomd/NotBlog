namespace Identity.Web.API.Application.Models;

public class RequestRegisterWithRoleModel
{
    public RequestRegisterWithRoleModel(string roleName, string? attribute, EnRoleAuthority? roleAuthority, EnRoleStatus? roleStatus)
    {
        RoleName = roleName;
        Attribute = attribute;
        RoleAuthority = roleAuthority;
        RoleStatus = roleStatus;
    }

    public string RoleName { get; }

    public string? Attribute { get; }

    public EnRoleAuthority? RoleAuthority { get; }

    public EnRoleStatus? RoleStatus { get; }

}

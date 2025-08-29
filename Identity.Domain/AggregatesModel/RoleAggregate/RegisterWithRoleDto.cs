namespace Identity.Domain.AggregatesModel.RoleAggregate;

public class RegisterWithRoleDto
{
    public RegisterWithRoleDto(string roleName, string? attribute, EnRoleAuthority roleAuthority = EnRoleAuthority.Uknown,
        EnRoleStatus roleStatus = EnRoleStatus.Disabled)
    {
        RoleName = roleName;
        Attribute = attribute;
        RoleAuthority = roleAuthority;
        RoleStatus = roleStatus;
    }

    public string RoleName { get; } = string.Empty;

    public string? Attribute { get; } = string.Empty;

    public EnRoleAuthority RoleAuthority { get; }

    public EnRoleStatus RoleStatus { get; }
}

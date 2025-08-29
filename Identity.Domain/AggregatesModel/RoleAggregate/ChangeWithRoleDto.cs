namespace Identity.Domain.AggregatesModel.RoleAggregate;

public class ChangeWithRoleDto
{
    public ChangeWithRoleDto(string changeRoleName, string changeRoleAttribute,
        EnRoleAuthority changeRoleAuthority,
        EnRoleStatus changeRoleStatus, List<ChangeWithRoleClaimDto>? changeRoleClaims)
    {
        ChangeRoleName = changeRoleName;
        ChangeRoleAttribute = changeRoleAttribute;
        ChangeRoleClaims = changeRoleClaims;
        ChangeRoleAuthority = changeRoleAuthority;
        ChangeRoleStatus = changeRoleStatus;
    }

    public string ChangeRoleName { get; }

    public string ChangeRoleAttribute { get; }

    public List<ChangeWithRoleClaimDto>? ChangeRoleClaims { get; } = new List<ChangeWithRoleClaimDto>();

    public EnRoleAuthority ChangeRoleAuthority { get; }

    public EnRoleStatus ChangeRoleStatus { get; }

}

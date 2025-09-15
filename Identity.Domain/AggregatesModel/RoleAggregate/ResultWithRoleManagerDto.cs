using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Domain.AggregatesModel.RoleAggregate;

public class ResultWithRoleManagerDto
{
    public ResultWithRoleManagerDto(string roleName, string roleAttribute, EnRoleAuthority enRoleAuthority, 
        EnRoleStatus enRoleStatus, IEnumerable<WithResultRoleClaimDto> withResultRoleClaimDtos)
    {
        RoleName = roleName;
        RoleAttribute = roleAttribute;
        EnRoleAuthority = enRoleAuthority;
        EnRoleStatus = enRoleStatus;
        WithResultRoleClaimDtos = withResultRoleClaimDtos;
    }

    public string RoleName { get; }

    public string RoleAttribute { get; }

    public EnRoleAuthority EnRoleAuthority { get; }

    public EnRoleStatus EnRoleStatus { get; }

    public IEnumerable<WithResultRoleClaimDto> WithResultRoleClaimDtos { get; } = Enumerable.Empty<WithResultRoleClaimDto>();
}

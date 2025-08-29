using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Domain.AggregatesModel.RoleAggregate
{
    public class ResultWithRoleDto
    {
        public ResultWithRoleDto(string roleName, string roleAttribute, IEnumerable<WithResultRoleClaimDto> claims)
        {
            RoleName = roleName;
            Claims = claims;
            RoleAttribute = roleAttribute;
        }

        public string RoleName { get; }

        public string RoleAttribute { get; } = string.Empty;

        public IEnumerable<WithResultRoleClaimDto> Claims { get; } = Enumerable.Empty<WithResultRoleClaimDto>();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Domain.AggregatesModel.RoleAggregate
{
    public class WithResultRoleClaimDto
    {
        public string ClaimType { get; }
        public string ClaimValue { get; }
        public WithResultRoleClaimDto(string claimType, string claimValue)
        {
            ClaimType = claimType;
            ClaimValue = claimValue;
        }
    }
}

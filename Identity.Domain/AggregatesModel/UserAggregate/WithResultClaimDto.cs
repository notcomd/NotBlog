using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Domain.AggregatesModel.UserAggregate
{
    public class WithResultClaimDto
    {
        public WithResultClaimDto(string claimType, string claimValue)
        {
            ClaimType = claimType;
            ClaimValue = claimValue;
        }

        public string ClaimType { get; }
         public string ClaimValue { get; }
    }
}

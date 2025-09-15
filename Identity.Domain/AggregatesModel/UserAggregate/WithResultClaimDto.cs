

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

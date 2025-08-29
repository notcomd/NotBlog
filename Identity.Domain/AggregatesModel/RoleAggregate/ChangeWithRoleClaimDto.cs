namespace Identity.Domain.AggregatesModel.RoleAggregate
{
    public class ChangeWithRoleClaimDto
    {
        public ChangeWithRoleClaimDto(string claimType, string claimValue)
        {
            ClaimType = claimType;
            ClaimValue = claimValue;
        }

        public string ClaimType { get; } = null!;
        public string ClaimValue { get; } = null!;
    }
}

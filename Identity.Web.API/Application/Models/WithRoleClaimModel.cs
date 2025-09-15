namespace Identity.Web.API.Application.Models
{
    public class WithRoleClaimModel
    {
        public string ClaimType { get; set; } = string.Empty;

        public string ClaimValue { get; set; } = string.Empty;


        public WithRoleClaimModel(string claimType, string claimValue)
        {
            ClaimType = claimType;
            ClaimValue = claimValue;
        }
    }
}

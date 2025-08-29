namespace Identity.Web.API.Application.Models
{
    public class ResponseChangeWithClaimModel
    {
        public ResponseChangeWithClaimModel(string claimType, string claimValue)
        {
            ClaimType = claimType;
            ClaimValue = claimValue;
        }

        public string ClaimType { get; }

        public string ClaimValue { get; }
    }
}

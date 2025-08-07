namespace Identity.Domain.AggregatesModel.UserAggregate;

public class UserClaim : Entity
{

    //public Guid UserClaimGuid { get; private set; }

    public Guid UserGuid { get; private set; }

    public string ClaimType { get; private set; } = string.Empty;

    public string ClaimValue { get; private set; } = string.Empty;

    public static UserClaim CreateByUserClaimAsync(Guid userGuid ,Claim claim)
    {

        if (claim == null)
        {
            throw new ArgumentNullException(nameof(claim), "Claim cannot be null");
        }
        return new UserClaim
        {            
            Id = Guid.CreateVersion7(),
            UserGuid=userGuid,
            ClaimType = claim.Type,
            ClaimValue = claim.Value
        };
        
    }


    public Claim ToClaim()
    {
        return new Claim(ClaimType, ClaimValue);
    }

    public void Initialize(Claim claim)
    {
        ClaimValue = claim.Value;
        ClaimType = claim.Type;
    }

}

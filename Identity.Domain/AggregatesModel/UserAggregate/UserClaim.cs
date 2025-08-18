namespace Identity.Domain.AggregatesModel.UserAggregate;

public class UserClaim : Entity
{

    public Guid UserGuid { get; init; }

    public string ClaimType { get; private set; } = string.Empty;

    public string ClaimValue { get; private set; } = string.Empty;

    public static UserClaim CreateByUserClaimAsync(Guid userGuid, string claimType, string claimValue)
    {
        if (userGuid == Guid.Empty)
            throw new ArgumentNullException(nameof(userGuid));
        ArgumentNullException.ThrowIfNullOrEmpty(nameof(claimType));
        ArgumentNullException.ThrowIfNullOrEmpty(nameof(claimValue));

        return new UserClaim
        {
            Id = Guid.CreateVersion7(),
            UserGuid = userGuid,
            ClaimType = claimType,
            ClaimValue = claimValue
        };

    }

    public static ValueTask<UserClaim> CreateByClaimAsync(Guid userGuid, string claimType, string claimValue)
    {
        return new ValueTask<UserClaim>(CreateByUserClaimAsync(userGuid, claimType, claimValue));
    }

    public Claim ToClaim()
    {
        return new Claim(ClaimType, ClaimValue);
    }

    public void UpdateClaim(string claimType, string claimValue)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(nameof(claimType));
        ArgumentNullException.ThrowIfNullOrEmpty(nameof(claimValue));
        ClaimType = claimType;
        ClaimValue = claimValue;
    }

}

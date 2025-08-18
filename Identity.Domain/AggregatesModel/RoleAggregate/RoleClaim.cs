namespace Identity.Domain.AggregatesModel.RoleAggregate;

public class RoleClaim : Entity
{

    public Guid RoleGuid { get; private set; }

    public string ClaimType { get; private set; } = string.Empty;

    public string ClaimValue { get; private set; } = string.Empty;


    public static ValueTask<RoleClaim> CreteByRoleClaimValueTask(Guid roleGuid,string claimType,string claimValue)
    {
        return new ValueTask<RoleClaim>(CreateByRoleClaimAsync(roleGuid, claimType, claimValue));
    }


    public static RoleClaim CreateByRoleClaimAsync(Guid roleGuid, string claimType, string claimValue)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(nameof(claimType));
        ArgumentNullException.ThrowIfNullOrEmpty(nameof(claimValue));
        if (roleGuid == Guid.Empty)
            throw new ArgumentNullException(nameof(roleGuid), "Role GUID cannot be empty");

        return new RoleClaim
        {
            Id = Guid.CreateVersion7(),
            RoleGuid = roleGuid,
            ClaimType = claimType,
            ClaimValue = claimValue
        };
    }


    public void AddRoleClaim(Claim claim)
    {
        if (claim == null)
            throw new ArgumentNullException(nameof(claim), "Claim cannot be null");

        ClaimType = claim.Type;
        ClaimValue = claim.Value;
    }


    public Claim ToClaim()
    {
        if (string.IsNullOrEmpty(ClaimType) || string.IsNullOrEmpty(ClaimValue))
            throw new InvalidOperationException("ClaimType and ClaimValue cannot be null or empty");

        return new Claim(ClaimType, ClaimValue);
    }
}

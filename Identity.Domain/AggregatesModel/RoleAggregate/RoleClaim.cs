using Microsoft.AspNetCore.Authentication;

namespace Identity.Domain.AggregatesModel.RoleAggregate;

public class RoleClaim : Entity
{


    public Guid RoleGuid { get; private set; }

    public string ClaimType { get; private set; } =string.Empty;

    public string ClaimValue { get; private set; }=string.Empty;


    public static ValueTask<RoleClaim> CreateByRoleClaimAsync(Guid roleGuid, Claim claim)
    {
        if (roleGuid == Guid.Empty)
            throw new ArgumentNullException(nameof(roleGuid), "Role GUID cannot be empty");
        if (claim == null)
            throw new ArgumentNullException(nameof(claim), "Claim cannot be null");
        var roleClaim = new RoleClaim
        {
            RoleGuid = roleGuid,
            ClaimType = claim.Type,
            ClaimValue = claim.Value
        };
        return new ValueTask<RoleClaim>(roleClaim);
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

namespace Identity.Domain.AggregatesModel.RoleAggregate;

public class RoleClaim : Entity
{

    public Guid RoleGuid { get; private set; }

    public string ClaimType { get; private set; } = string.Empty;

    public string ClaimValue { get; private set; } = string.Empty;


    public static ValueTask<RoleClaim> CreteByRoleClaimValueTask(Guid roleGuid,string claimType,string claimValue)
    {
        return new ValueTask<RoleClaim>(CreateByRoleClaim(roleGuid, claimType, claimValue));
    }


    public static RoleClaim CreateByRoleClaim(Guid roleGuid, string claimType, string claimValue)
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


    public void AddRoleClaim(string claimType,string claimValue)
    {
        if(string.IsNullOrEmpty(claimType) || string.IsNullOrEmpty(claimValue))
            throw new ArgumentNullException($"{claimType} or {claimValue} is null!");

        ClaimType = claimType;
        ClaimValue = claimValue;
    }

    public Claim ToClaim()
    {
        if (string.IsNullOrEmpty(ClaimType) || string.IsNullOrEmpty(ClaimValue))
            throw new InvalidOperationException("ClaimType and ClaimValue cannot be null or empty");

        return new Claim(ClaimType, ClaimValue);
    }

    public void ChangeByRoleClaim(string claimType,string claimValue)
    {
        if (string.IsNullOrEmpty(ClaimType) && string.IsNullOrEmpty(ClaimValue))
            throw new InvalidOperationException($"{claimType}And {claimValue} is null!");

        this.ClaimValue= claimValue;
        this.ClaimType= claimType;
    }
}

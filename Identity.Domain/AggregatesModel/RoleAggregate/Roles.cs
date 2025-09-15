namespace Identity.Domain.AggregatesModel.RoleAggregate;

public class Roles : Entity, IAggregateRoot
{

    public string RoleName { get; private set; }

    public string? Attribute { get; private set; }

    private List<RoleClaim> _roleClaim = new List<RoleClaim>();

    public IEnumerable<RoleClaim> RoleClaims => _roleClaim.AsReadOnly();

    public EnRoleAuthority RoleAuthority { get; private set; }

    public EnRoleStatus RoleStatus { get; private set; }

    public DateTimeOffset CreateRoleTime { get; init; }


    protected Roles()
    {

    }

    public Roles(string roleName, string? attribute, DateTimeOffset dateTimeOffset,
        EnRoleAuthority roleAuthority = EnRoleAuthority.User, EnRoleStatus roleStatus = EnRoleStatus.Normal)
    {
        if (string.IsNullOrEmpty(roleName))
            throw new ArgumentNullException(nameof(roleName), "Role name cannot be null or empty");

        Id = Guid.CreateVersion7();
        RoleName = roleName;
        Attribute = attribute;
        RoleAuthority = roleAuthority;
        RoleStatus = roleStatus;
        CreateRoleTime = dateTimeOffset;
        AddDomainEvent(new CreatedByRoleDomainEvent(Id, RoleName, Attribute));

    }

    public static ValueTask<Roles> CreateAsync(string roleName, DateTimeOffset dateTimeOffset,
        string? attribute = null, EnRoleAuthority roleAuthority = EnRoleAuthority.User, EnRoleStatus roleStatus = EnRoleStatus.Normal)
    {
        return new ValueTask<Roles>(new Roles(roleName, attribute, dateTimeOffset, roleAuthority, roleStatus));
    }

    public void ChangeByRoleAuthority(EnRoleAuthority roleAuthority)
    {
        RoleAuthority = roleAuthority;
        AddDomainEvent(new RoleAuthorityChangedDomainEvent(Id, roleAuthority));
    }

    public void ChangeByRoleName(string roleName)
    {
        if (string.IsNullOrEmpty(roleName))
            throw new ArgumentNullException(nameof(roleName), "Role name cannot be null or empty");
        RoleName = roleName;
        AddDomainEvent(new RoleNameChangedDomainEvent(Id, roleName));
    }

    public void AddRoleClaim(RoleClaim roleClaim)
    {
        if (roleClaim is null)
            throw new ArgumentNullException(nameof(roleClaim), "Role claim cannot be null");

        if (_roleClaim.Any(rc => rc.ClaimType == roleClaim.ClaimType))
            throw new InvalidOperationException($"Claim with type {roleClaim.ClaimType} already exists.");

        _roleClaim.Add(roleClaim);
        AddDomainEvent(new RoleClaimAddedDomainEvent(Id, roleClaim.ClaimType, roleClaim.ClaimValue));
    }

    public void ChangeByRoleStatus(EnRoleStatus roleStatus)
    {
        RoleStatus = roleStatus;
        AddDomainEvent(new RoleStatusChangedDomainEvent(Id, roleStatus));
    }

    public void ChangeByRoleClaim(IEnumerable<RoleClaim> roleClaims)
    {
        if (roleClaims is null || !roleClaims.Any())
            throw new ArgumentNullException(nameof(roleClaims), "Role claims cannot be null or empty");
        _roleClaim.Clear();
        _roleClaim.AddRange(roleClaims);
        AddDomainEvent(new RoleClaimsChangedDomainEvent(Id, roleClaims));
    }

}
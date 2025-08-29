using Identity.Domain.Events;

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

    public static ValueTask<Roles> CreateByRoleAsyncTask(string roleName, DateTimeOffset dateTimeOffset,
        string? attribute = null, EnRoleAuthority roleAuthority = EnRoleAuthority.User, EnRoleStatus roleStatus = EnRoleStatus.Normal)
    {
        return new ValueTask<Roles>(new Roles(roleName, attribute, dateTimeOffset, roleAuthority, roleStatus));
    }

    public void ChangeByRoleAuthority(EnRoleAuthority roleAuthority)
    {
        RoleAuthority = roleAuthority;
    }

    public void ChangeByRoleName(string roleName)
    {
        if (string.IsNullOrEmpty(roleName))
            throw new ArgumentNullException(nameof(roleName), "Role name cannot be null or empty");
        RoleName = roleName;
    }

    public void AddRoleClaim(RoleClaim roleClaim)
    {
        if (roleClaim is null)
            throw new ArgumentNullException(nameof(roleClaim), "Role claim cannot be null");
        _roleClaim.Add(roleClaim);
    }

    public void ChangeByRoleStatus(EnRoleStatus roleStatus) => RoleStatus = roleStatus;

    public IEnumerable<Claim>? RoleClaimToClaim(IEnumerable<RoleClaim> roleClaim)
    {

        if (roleClaim is null || !roleClaim.Any())
            throw new ArgumentNullException(nameof(roleClaim), "Role claims cannot be null or empty");

        var entity = roleClaim
            .Where(en => string.IsNullOrEmpty(en.ClaimValue) || string.IsNullOrEmpty(en.ClaimType));

        if (entity.Any())
            throw new InvalidOperationException("ClaimType and ClaimValue cannot be null or empty");

        return roleClaim.Select(en=>en.ToClaim());


    }

    public void ChangeByRoleClaim(IEnumerable<RoleClaim> roleClaims)
    {
        if (roleClaims is null || !roleClaims.Any())
            throw new ArgumentNullException(nameof(roleClaims), "Role claims cannot be null or empty");
        _roleClaim.Clear();
        _roleClaim.AddRange(roleClaims);
    }

}
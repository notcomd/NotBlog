using Identity.Domain.AggregatesModel.UserAggregate;
using Identity.Domain.Events;

namespace Identity.Domain.AggregatesModel.RoleAggregate;

public class Roles : Entity, IAggregateRoot
{

    public string RoleName { get; private set; }

    public string? Attribute { get; private set; }

    private List<RoleClaim> _roleClaim = new List<RoleClaim>();

    public IEnumerable<RoleClaim> RoleClaims => _roleClaim.AsReadOnly();

    public EnumRoleAuthority RoleAuthority { get; private set; }

    public EnumRoleStatus RoleStatus { get; private set; }

    public DateTimeOffset CreateRoleTime { get; init; }


    protected Roles()
    {
        Id = Guid.CreateVersion7();
    }

    public static Roles CreateByRoleAsync(string roleName, string? attribute, DateTimeOffset dateTimeOffset,
        EnumRoleAuthority roleAuthority = EnumRoleAuthority.User, EnumRoleStatus roleStatus = EnumRoleStatus.Normal)
    {
        if (string.IsNullOrEmpty(roleName))
            throw new ArgumentNullException(nameof(roleName), "Role name cannot be null or empty");
        var role = new Roles
        {
            RoleName = roleName,
            Attribute = attribute,
            RoleAuthority = roleAuthority,
            RoleStatus = roleStatus,
            CreateRoleTime = dateTimeOffset
        };
        role.AddDomainEvent(new CreatedByRoleDomainEvent(role.Id, role.RoleName, role.Attribute));
        return role;
    }

    public static ValueTask<Roles> CreateByRoleAsyncTask(string roleName, DateTimeOffset dateTimeOffset, string? attribute = null, EnumRoleAuthority roleAuthority = EnumRoleAuthority.User, EnumRoleStatus roleStatus = EnumRoleStatus.Normal)
    {
        return new ValueTask<Roles>(CreateByRoleAsync(roleName,  attribute, dateTimeOffset, roleAuthority, roleStatus));
    }

    public void ChangeByRoleAuthority(EnumRoleAuthority roleAuthority)
    {
        RoleAuthority = roleAuthority;
    }

    public void AddRoleClaim(RoleClaim roleClaim)
    {
        if (roleClaim is null)
            throw new ArgumentNullException(nameof(roleClaim), "Role claim cannot be null");
        _roleClaim.Add(roleClaim);
    }

    public void ChangeByRoleStatus(EnumRoleStatus roleStatus) => RoleStatus = roleStatus;

    public IEnumerable<Claim>? RoleClaimToClaim(IEnumerable<RoleClaim> roleClaim)
    {
        if (roleClaim is not null && roleClaim.Any())
        {
            foreach (var userClaim in roleClaim)
            {
                if (string.IsNullOrEmpty(userClaim.ClaimType) || string.IsNullOrEmpty(userClaim.ClaimValue))
                {
                    throw new InvalidOperationException("User claim type and value cannot be null or empty");
                }
                yield return userClaim.ToClaim();
            }
        }
        else
        {
            throw new ArgumentNullException(nameof(roleClaim), "User claims cannot be null or empty");
        }

    }

}
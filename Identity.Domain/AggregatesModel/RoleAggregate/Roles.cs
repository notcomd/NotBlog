using Identity.Domain.Events;

namespace Identity.Domain.AggregatesModel.RoleAggregate;

public class Roles : Entity, IAggregateRoot
{
       

    public string RoleName { get; private set; }

    public string? Attribute { get; private set; }

    private List<RoleClaim> _roleClaim { get; set; }

    public IEnumerable<RoleClaim> RoleClaims => _roleClaim.AsReadOnly();

    public EnumRoleAuthority RoleAuthority { get; private set; }

    public EnumRoleStatus RoleStatus { get; private set; }

    public DateTimeOffset CreateRoleTime { get; init; }


    protected Roles()
    {
        Id = Guid.CreateVersion7();
    }



    public static Task<Roles> CreateByRoleAsync(string roleName, DateTimeOffset dateTimeOffset, string? attribute = null, EnumRoleAuthority roleAuthority = EnumRoleAuthority.User, EnumRoleStatus roleStatus = EnumRoleStatus.Normal)
    {
        if (string.IsNullOrEmpty(roleName))
            throw new ArgumentNullException(nameof(roleName), "Role name cannot be null or empty");
        var role = new Roles
        {
            Id = Guid.CreateVersion7(),
            RoleName = roleName,
            Attribute = attribute,
            RoleAuthority = roleAuthority,
            RoleStatus = roleStatus,
            CreateRoleTime = dateTimeOffset
        };
        role.AddDomainEvent(new CreateByRoleStartEvent(role.Id, role.RoleName, role.Attribute));
        return Task.FromResult(role);
    }



    public void SetOrResetByRoleAuthority(EnumRoleAuthority roleAuthority)
    {
        RoleAuthority = roleAuthority;
    }



    public void AddRoleClaim(RoleClaim roleClaim)
    {
        if (roleClaim is null)
            throw new ArgumentNullException(nameof(roleClaim), "Role claim cannot be null");

        _roleClaim ??= new List<RoleClaim>();
        _roleClaim.Add(roleClaim);   
    }


    public void SetOrResetByRoleStatus(EnumRoleStatus roleStatus)
    {
        RoleStatus = roleStatus;
    }


}
namespace Identity.Domain.AggregatesModel.RoleAggregate;

public class Roles : Entity, IAggregateRoot
{

    protected Roles()
    {
    }

    public Roles(Guid userGuid, string roleName, string? attribute = null, RoleAuthority roleAuthority = RoleAuthority.User, RoleStatus roleStatus = RoleStatus.Normal)
    {
        RoleGuid = Guid.CreateVersion7();
        UserGuid = userGuid;
        RoleName = roleName ?? throw new ArgumentNullException(nameof(roleName), "Role name cannot be null");
        Attribute = attribute;
        RoleAuthority = roleAuthority;
        RoleStatus = roleStatus;
        CreateRole = DateTimeOffset.UtcNow;
    }

    public Guid RoleGuid { get; private set; }

    public Guid UserGuid { get; private set; }

    public User User { get; private set; }

    public string RoleName { get; private set; }

    public string? Attribute { get; private set; }

    public RoleAuthority RoleAuthority { get; private set; }

    public RoleStatus RoleStatus { get; private set; }

    public DateTimeOffset CreateRole { get; init; }

    public void ResetByRoleAuthority(RoleAuthority roleAuthority)
    {
        RoleAuthority = roleAuthority;
    }


    public void ResetByRoleStatus(RoleStatus roleStatus)
    {
        RoleStatus = roleStatus;
    }
}
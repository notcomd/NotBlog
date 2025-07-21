namespace Identity.Domain.AggregatesModel.RoleAggregate;

public class Roles : Entity, IAggregateRoot
{

    protected Roles()
    {
    }



    public static Roles CreateRole(string roleName, string? attribute = null, RoleAuthority roleAuthority = RoleAuthority.User, RoleStatus roleStatus = RoleStatus.Normal)
    {
        if (string.IsNullOrEmpty(roleName))
            throw new ArgumentNullException(nameof(roleName), "Role name cannot be null or empty");
        return new Roles
        {
            RoleGuid = Guid.CreateVersion7(),
            RoleName = roleName,
            Attribute = attribute,
            RoleAuthority = roleAuthority,
            RoleStatus = roleStatus,
            CreateRoleTime = DateTimeOffset.UtcNow
        };
    }


    public Guid RoleGuid { get; private set; }

    public string RoleName { get; private set; }

    public string? Attribute { get; private set; }

    public RoleAuthority RoleAuthority { get; private set; }

    public RoleStatus RoleStatus { get; private set; }

    public DateTimeOffset CreateRoleTime { get; init; }



    public void ResetByRoleAuthority(RoleAuthority roleAuthority)
    {
        RoleAuthority = roleAuthority;
    }


    public void ResetByRoleStatus(RoleStatus roleStatus)
    {
        RoleStatus = roleStatus;
    }
}
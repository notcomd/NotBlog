using DomainCommonst;

using Identity.Domain.Events;

namespace Identity.Domain.AggregatesModel.RoleAggregate;

public class Roles : Entity, IAggregateRoot
{

    protected Roles()
    {
    }



    public  static Task<Roles> CreateByRoleAsync(string roleName, DateTimeOffset dateTimeOffset, string? attribute = null,  EnumRoleAuthority roleAuthority = EnumRoleAuthority.User, EnumRoleStatus roleStatus = EnumRoleStatus.Normal)
    {
        if (string.IsNullOrEmpty(roleName))
            throw new ArgumentNullException(nameof(roleName), "Role name cannot be null or empty");
       var role= new Roles
        {
            RoleGuid = Guid.CreateVersion7(),
            RoleName = roleName,
            Attribute = attribute,
            RoleAuthority = roleAuthority,
            RoleStatus = roleStatus,
            CreateRoleTime = dateTimeOffset
        };
        role.AddDomainEvent(new CreateByRoleStartEvent(role.RoleGuid, role.RoleName, role.Attribute));
        return Task.FromResult(role);
    }


    public Guid RoleGuid { get; private set; }

    public string RoleName { get; private set; }

    public string? Attribute { get; private set; }

    public EnumRoleAuthority RoleAuthority { get; private set; }

    public EnumRoleStatus RoleStatus { get; private set; }

    public DateTimeOffset CreateRoleTime { get; init; }



    public void ResetByRoleAuthority(EnumRoleAuthority roleAuthority)
    {
        RoleAuthority = roleAuthority;
    }


    public void ResetByRoleStatus(EnumRoleStatus roleStatus)
    {
        RoleStatus = roleStatus;
    }
}
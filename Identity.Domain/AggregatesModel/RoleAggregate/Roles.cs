namespace Identity.Domain.AggregatesModel.RoleAggregate;

public class Roles : Entity, IAggregateRoot
{

    public Guid RoleGuid { get; private set; }
    
    public HashSet<Guid> UserGuid { get; private set; }
    
    public string RoleName { get; private set; }

    public string? Attribute { get; private set; }
    
    
    public string RoleCode{get; private set;}

    public RoleAuthority RoleAuthority { get; private set; }
    
    public RoleStatus RoleStatus { get; private set; }

    public bool IsDeleted{get; private set;}
    
    public DateTimeOffset CreateRole { get; init; }
    
    public HashSet<RolePermission> RolePermission { get; private set; }

    protected Roles()
    {
        RoleGuid = Guid.CreateVersion7();
        CreateRole = DateTime.UtcNow;
        UserGuid = new HashSet<Guid>();
        CreateRole = DateTimeOffset.UtcNow;
        IsDeleted = false;
        RolePermission = new HashSet<RolePermission>();
        RoleStatus = RoleStatus.Normal;
    }

    public Roles(HashSet<Guid> userGuid, string roleName, string? attribute = null, RoleAuthority roleAuthority = RoleAuthority.User, 
        RoleStatus roleStatus = RoleStatus.Normal):this()
    {
        UserGuid = userGuid;
        RoleName = roleName ?? throw new ArgumentNullException(nameof(roleName), "Role name cannot be null");
        Attribute = attribute;
        RoleAuthority = roleAuthority;
        RoleStatus = roleStatus;
        //CreateRole = DateTimeOffset.UtcNow;
    }
    
    


    public void ResetByRoleAuthority(RoleAuthority roleAuthority)
    {
        RoleAuthority = roleAuthority;
    }


    public void ResetByRoleStatus(RoleStatus roleStatus)
    {
        RoleStatus = roleStatus;
    }

    public class RoleBuilder
    {
        private Guid _userGuid;
        private string _roleName;
        private string _roleCode;
        private RoleAuthority _roleAuthority;
        private RoleStatus _roleStatus;
        
    }
    
    
}
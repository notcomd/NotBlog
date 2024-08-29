namespace Identity.Domain.Entities;

public class UserRole:IAggregateRoot
{
    public Guid UserRoleGuid { get; init; }

    public string RoleName { get; private set; } = null!;
    /// <summary>
    /// 应用该角色人数
    /// </summary>
    public long Roles { get; private set; }
    
    
    private UserRole(){}

    public UserRole(string roleName)
    {
        UserRoleGuid = new Guid();
        RoleName = roleName;
    }

    private void AddByRolesAsync()
    {
        Roles++;
    }

    public ValueTask<bool> IsRoleAsync(string roleName)
    {
        return new ValueTask<bool>(RoleName == roleName);
    }
    
    
}
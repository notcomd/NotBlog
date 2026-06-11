namespace Identity.Domain.Entities.RoleAggregate;

public class RoleGroup : Entity, IAggregateRoot
{
    public RoleGroup(string roleGroupName, string roleGroupCode) : this()
    {
        if (string.IsNullOrWhiteSpace(roleGroupName))
            throw new ArgumentException("Role group name cannot be null or empty", nameof(roleGroupName));
        if (string.IsNullOrWhiteSpace(roleGroupCode))
            throw new ArgumentException("Role group code cannot be null or empty", nameof(roleGroupCode));
        RoleGroupName = roleGroupName;
        RoleGroupCode = roleGroupCode;
        Permissions = new List<Permission>();
    }

    protected RoleGroup()
    {
        RoleGroupGuid = Guid.CreateVersion7();
        Roles = new List<Roles>();
        Permissions = new List<Permission>();
        CreatedRoleGroup = DateTimeOffset.UtcNow;
        IsDeleted = false;
    }

    /// <summary>
    /// 角色组ID
    /// </summary>
    public Guid RoleGroupGuid { get; init; }

    /// <summary>
    /// 角色组名称
    /// </summary>
    public string RoleGroupName { get; private set; } = null!;

    /// <summary>
    /// 角色组编码
    /// </summary>
    public string RoleGroupCode { get; private set; } = null!;

    /// <summary>
    /// 角色组下的角色
    /// </summary>
    public ICollection<Roles>? Roles { get; private set; }

    /// <summary>
    /// 角色组下的权限
    /// </summary>
    public ICollection<Permission> Permissions { get; private set; }

    /// <summary>
    /// 创建角色组时间
    /// </summary>
    public DateTimeOffset CreatedRoleGroup { get; init; }

    /// <summary>
    /// 是否已删除
    /// </summary>
    public bool IsDeleted { get; private set; }

    public void ChangeRoleGroup(string roleGroupName, string roleGroupCode, ICollection<Roles> roles)
    {
        RoleGroupName = roleGroupName;
        RoleGroupCode = roleGroupCode;
        Roles = roles;
    }

    public void RemoveRole(Roles role)
    {
        if (!Roles!.Contains(role))
            throw new ArgumentException("Role not found");
        Roles.Remove(role);
    }

    public void AddRole(Roles role)
    {
        if (Roles!.Contains(role))
            throw new ArgumentException("Role already exists");
        Roles.Add(role);
    }

    public void SoftDelete(bool deleted)
    {
        IsDeleted = deleted;
    }

    public void UpdateRoleGroupInfo(string roleGroupName, string roleGroupCode)
    {
        if (!string.IsNullOrWhiteSpace(roleGroupName))
            RoleGroupName = roleGroupName;
        if (!string.IsNullOrWhiteSpace(roleGroupCode))
            RoleGroupCode = roleGroupCode;
    }

    public void AddPermission(Permission permission)
    {
        if (Permissions.Contains(permission))
            throw new ArgumentException("Permission already exists");
        Permissions.Add(permission);
    }

    public void RemovePermission(Permission permission)
    {
        if (!Permissions.Contains(permission))
            throw new ArgumentException("Permission not found");
        Permissions.Remove(permission);
    }
}
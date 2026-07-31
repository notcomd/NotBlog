namespace Identity.Domain.Entities.RoleAggregate;

/// <summary>
/// 角色实体（聚合根）
/// 
/// 与 RoleGroup 的关系：通过 RoleGroupGuids（Guid 列表）引用所属角色组 ID，
/// 不再直接持有 RoleGroup 对象引用，避免聚合根间双向循环依赖。
/// </summary>
public class Roles : Entity, IAggregateRoot
{
    protected Roles()
    {
        RoleGuid = Guid.CreateVersion7();
        UserGuid = new HashSet<Guid>();
        Permissions = new List<Permission>();
        RoleGroupGuids = new List<Guid>();
        CreateRole = DateTimeOffset.UtcNow;
        IsDeleted = false;
        RoleStatus = RoleStatus.Normal;
        RoleAuthority = RoleAuthority.User;
    }

    public Roles(
        string roleName,
        string roleCode,
        RoleAuthority roleAuthority = RoleAuthority.User,
        RoleStatus roleStatus = RoleStatus.Normal,
        string? attribute = null) : this()
    {
        if (string.IsNullOrWhiteSpace(roleName))
            throw new ArgumentException("Role name cannot be null or empty", nameof(roleName));

        if (string.IsNullOrWhiteSpace(roleCode))
            throw new ArgumentException("Role code cannot be null or empty", nameof(roleCode));
        RoleName = roleName;
        RoleCode = roleCode;
        Attribute = attribute;
        RoleAuthority = roleAuthority;
        RoleStatus = roleStatus;
    }

    public Guid RoleGuid { get; private set; }

    public HashSet<Guid> UserGuid { get; private set; }

    public string RoleName { get; private set; }

    public string? Attribute { get; private set; }

    public string RoleCode { get; private set; }

    public RoleAuthority RoleAuthority { get; private set; }

    public RoleStatus RoleStatus { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset CreateRole { get; init; }

    /// <summary>
    /// 角色直连权限
    /// </summary>
    public ICollection<Permission> Permissions { get; private set; }

    /// <summary>
    /// 角色所属组的 Guid 列表（通过 ID 引用 RoleGroup 聚合根，避免双向循环依赖）
    /// </summary>
    public List<Guid> RoleGroupGuids { get; private set; }

    public void AddUserGuid(Guid userGuid)
    {
        if (!UserGuid.Add(userGuid))
            throw new InvalidOperationException("UserGuid already exists");
    }

    public void RemoveUserGuid(Guid userGuid)
    {
        if (!UserGuid.Remove(userGuid))
            throw new InvalidOperationException("UserGuid does not exist");
    }

    public void AddPermission(Permission permission)
    {
        if (!Permissions.Contains(permission))
            Permissions.Add(permission);
    }

    public void RemovePermission(Permission permission)
    {
        if (!Permissions.Remove(permission))
            throw new InvalidOperationException("Permission does not exist");
    }

    /// <summary>
    /// 将角色加入指定的角色组
    /// </summary>
    public void AddToRoleGroup(Guid roleGroupGuid)
    {
        if (RoleGroupGuids.Contains(roleGroupGuid))
            throw new InvalidOperationException("Role group already assigned");
        RoleGroupGuids.Add(roleGroupGuid);
    }

    /// <summary>
    /// 将角色从角色组中移除
    /// </summary>
    public void RemoveFromRoleGroup(Guid roleGroupGuid)
    {
        if (!RoleGroupGuids.Remove(roleGroupGuid))
            throw new InvalidOperationException("Role group not found");
    }

    public void ResetByRoleAuthority(RoleAuthority roleAuthority)
    {
        RoleAuthority = roleAuthority;
    }

    public void UpdateRoleInfo(string roleName, string? attribute)
    {
        if (!string.IsNullOrWhiteSpace(roleName))
            RoleName = roleName;
        Attribute = attribute;
    }

    public void ResetByRoleStatus(RoleStatus roleStatus)
    {
        RoleStatus = roleStatus;
    }

    public static class RoleFactory
    {
        public static Roles CreateAdminRole()
        {
            return new Roles("Administrator", "ADMIN", RoleAuthority.Admin, RoleStatus.Normal);
        }

        public static Roles CreateUserRole()
        {
            return new Roles("User", "USER", RoleAuthority.User, RoleStatus.Normal);
        }

        public static Roles CreateGuestRole()
        {
            return new Roles("Guest", "GUEST", RoleAuthority.Guest, RoleStatus.Normal);
        }
    }
}

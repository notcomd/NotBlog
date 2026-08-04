namespace Identity.Domain.Entities.RoleAggregate;

public class RoleGroup : Entity<int>, IAggregateRoot
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
        RoleGuids = new List<Guid>();
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
    /// 角色组下包含的角色 Guid 列表（通过 ID 引用 Roles 聚合根，避免双向循环依赖）
    /// </summary>
    public List<Guid> RoleGuids { get; private set; }

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

    public void ChangeRoleGroup(string roleGroupName, string roleGroupCode, List<Guid> roleGuids)
    {
        RoleGroupName = roleGroupName;
        RoleGroupCode = roleGroupCode;
        RoleGuids = roleGuids;
    }

    public void AddRole(Guid roleGuid)
    {
        if (RoleGuids.Contains(roleGuid))
            throw new ArgumentException("Role already exists in group");
        RoleGuids.Add(roleGuid);
    }

    public void RemoveRole(Guid roleGuid)
    {
        if (!RoleGuids.Remove(roleGuid))
            throw new ArgumentException("Role not found in group");
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

namespace Identity.Domain.Entities.RoleAggregate;

public class Permission : Entity<Guid>
{
    protected Permission()
    {
        PermissionId = Guid.CreateVersion7();
        Roles = new HashSet<Roles>();
        RoleGroups = new HashSet<RoleGroup>();
        CreatedAt = DateTime.UtcNow;
        IsDeleted = false;
    }

    public Permission(
        string permissionCode,
        string permissionName,
        string permissionType,
        string? menuPath = null,
        string? apiMethod = null,
        string? apiUrl = null) : this()
    {
        if (string.IsNullOrWhiteSpace(permissionCode))
            throw new ArgumentException("Permission code cannot be null or empty", nameof(permissionCode));

        if (string.IsNullOrWhiteSpace(permissionName))
            throw new ArgumentException("Permission name cannot be null or empty", nameof(permissionName));

        PermissionCode = permissionCode;
        PermissionName = permissionName;
        PermissionType = permissionType;
        MenuPath = menuPath;
        ApiMethod = apiMethod;
        ApiUrl = apiUrl;
    }

    /// <summary>
    /// 权限ID
    /// </summary>
    public Guid PermissionId { init; get; }

    /// <summary>
    /// 权限编码
    /// </summary>
    public string PermissionCode { private set; get; } = null!;

    /// <summary>
    /// 权限名称
    /// </summary>
    public string PermissionName { private set; get; } = null!;

    /// <summary>
    /// 权限类型
    /// </summary>
    public string PermissionType { private set; get; } = null!;

    /// <summary>
    /// 菜单路径
    /// </summary>
    public string? MenuPath { private set; get; }

    /// <summary>
    /// API方法
    /// </summary>
    public string? ApiMethod { private set; get; }

    /// <summary>
    /// API URL
    /// </summary>
    public string? ApiUrl { private set; get; }

    /// <summary>
    /// 是否已删除
    /// </summary>
    public bool IsDeleted { private set; get; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { private set; get; }

    /// <summary>
    /// 角色
    /// </summary>
    public ICollection<Roles> Roles { get; private set; }

    /// <summary>
    /// 角色组
    /// </summary>
    public ICollection<RoleGroup> RoleGroups { get; private set; }


    public void ChangePermission(string permissionCode, string permissionName, string permissionType
        , string menuPath, string apiMethod, string apiUrl)
    {
        if (!string.IsNullOrEmpty(permissionCode))
            PermissionCode = permissionCode;
        if (!string.IsNullOrEmpty(permissionName))
            PermissionName = permissionName;
        if (!string.IsNullOrEmpty(permissionType))
            PermissionType = permissionType;
        if (!string.IsNullOrEmpty(menuPath))
            MenuPath = menuPath;
        if (!string.IsNullOrEmpty(apiMethod))
            ApiMethod = apiMethod;
        if (!string.IsNullOrEmpty(apiUrl))
            ApiUrl = apiUrl;
    }

    public void SoftDelete(bool deleted)
    {
        IsDeleted = deleted;
    }


    public bool IsMenuPermission()
    {
        return !string.IsNullOrEmpty(MenuPath) && string.IsNullOrEmpty(ApiUrl);
    }

    public bool IsApiPermission()
    {
        return !string.IsNullOrEmpty(ApiUrl) && !string.IsNullOrEmpty(ApiMethod);
    }
}
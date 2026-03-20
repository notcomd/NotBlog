namespace Identity.Domain.AggregatesModel.RoleAggregate;

public class RolePermission
{
    protected RolePermission()
    {
        RolePermissionId = Guid.CreateVersion7();
        CreatedAt = DateTime.UtcNow;
        IsDeleted = false;
    }

    public RolePermission(
        string permissionCode,
        string permissionName,
        string permissionType,
        Guid? parentId = null,
        string? menuPath = null,
        string? apiMethod = null,
        string? apiUrl = null) : this()
    {
        if (string.IsNullOrWhiteSpace(permissionCode))
            throw new ArgumentException("Permission code cannot be null or empty", nameof(permissionCode));

        if (string.IsNullOrWhiteSpace(permissionName))
            throw new ArgumentException("Permission name cannot be null or empty", nameof(permissionName));

        ParentId = parentId;
        PermissionCode = permissionCode;
        PermissionName = permissionName;
        PermissionType = permissionType;
        MenuPath = menuPath;
        ApiMethod = apiMethod;
        ApiUrl = apiUrl;
    }


    public Guid RolePermissionId { init; get; }

    public Guid? ParentId { private set; get; }

    public Guid RoleId { private set; get; }

    public string PermissionCode { private set; get; }

    public string PermissionName { private set; get; }

    public string PermissionType { private set; get; }

    public string? MenuPath { private set; get; }

    public string? ApiMethod { private set; get; }

    public string? ApiUrl { private set; get; }

    public bool IsDeleted { private set; get; }

    public DateTime CreatedAt { private set; get; }

    public Roles Roles { private set; get; }

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
    
    public void BindToRole(Roles roles)
    {
        Roles = roles;
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
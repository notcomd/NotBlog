namespace Identity.Domain.Entities.RoleAggregate;

/// <summary>
/// 权限实体（树状结构，非聚合根）
///
/// 树形权限模型：节点经 ParentId 自引用构成权限树。
/// - 目录/菜单节点（PermissionType.Menu）：模块分组，可含子节点；
/// - 按钮（Button）/ API（Api）节点：叶子权限，由网关按 PermissionCode 精确匹配。
///
/// 授权语义（与 PermissionChecker 前缀段匹配配合）：
/// 授予某节点权限 = 自动拥有其全部子孙节点权限。
/// 例：角色授权 api:tweet（目录），则该角色自动放行 api:tweet:read / api:tweet:create 等全部子孙。
/// 现有按叶子逐码授权的数据不受影响（精确匹配仍命中）。
/// </summary>
public class Permission : Entity<Guid>
{
    protected Permission()
    {
        PermissionId = Guid.CreateVersion7();
        Children = new List<Permission>();
        Roles = new HashSet<Roles>();
        RoleGroups = new HashSet<RoleGroup>();
        CreatedAt = DateTime.UtcNow;
        IsDeleted = false;
    }

    /// <summary>
    /// 创建权限节点
    /// </summary>
    /// <param name="permissionCode">权限编码（创建后不可变更；目录节点为其子孙 code 的段前缀，如 api:tweet）</param>
    /// <param name="permissionName">权限名称</param>
    /// <param name="permissionType">权限类型</param>
    /// <param name="parentId">父节点 ID（null = 根节点）</param>
    /// <param name="url">菜单路径或接口地址</param>
    /// <param name="icon">图标</param>
    /// <param name="sortOrder">排序号（同级内升序）</param>
    public Permission(
        string permissionCode,
        string permissionName,
        PermissionType permissionType,
        Guid? parentId = null,
        string? url = null,
        string? icon = null,
        int sortOrder = 0) : this()
    {
        if (string.IsNullOrWhiteSpace(permissionCode))
            throw new ArgumentException("Permission code cannot be null or empty", nameof(permissionCode));

        if (string.IsNullOrWhiteSpace(permissionName))
            throw new ArgumentException("Permission name cannot be null or empty", nameof(permissionName));

        if (parentId == PermissionId)
            throw new ArgumentException("不能将权限挂到自己名下", nameof(parentId));

        PermissionCode = permissionCode.Trim();
        PermissionName = permissionName.Trim();
        PermissionType = permissionType;
        ParentId = parentId;
        Url = url;
        Icon = icon;
        SortOrder = sortOrder;
    }

    /// <summary>
    /// 权限ID
    /// </summary>
    public Guid PermissionId { get; init; }

    /// <summary>
    /// 权限编码（树内唯一；子孙节点 code 以此为段前缀）
    /// </summary>
    public string PermissionCode { get; private set; } = null!;

    /// <summary>
    /// 权限名称
    /// </summary>
    public string PermissionName { get; private set; } = null!;

    /// <summary>
    /// 权限类型（目录/按钮/API）
    /// </summary>
    public PermissionType PermissionType { get; private set; }

    /// <summary>
    /// 父节点 ID（null = 根节点）
    /// </summary>
    public Guid? ParentId { get; private set; }

    /// <summary>
    /// 菜单路径或接口地址（可空）
    /// </summary>
    public string? Url { get; private set; }

    /// <summary>
    /// 图标（可空）
    /// </summary>
    public string? Icon { get; private set; }

    /// <summary>
    /// 同级排序号（升序）
    /// </summary>
    public int SortOrder { get; private set; }

    /// <summary>
    /// 是否已删除
    /// </summary>
    public bool IsDeleted { get; private set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// 父节点导航
    /// </summary>
    public Permission? Parent { get; private set; }

    /// <summary>
    /// 子节点集合
    /// </summary>
    public ICollection<Permission> Children { get; private set; }

    /// <summary>
    /// 直连此权限的角色
    /// </summary>
    public ICollection<Roles> Roles { get; private set; }

    /// <summary>
    /// 直连此权限的角色组
    /// </summary>
    public ICollection<RoleGroup> RoleGroups { get; private set; }

    /// <summary>
    /// 更新基本信息（PermissionCode 创建后不可变更；Url/Icon 传 null 表示不修改，传空串表示清除）
    /// </summary>
    public void ChangePermission(
        string permissionName,
        PermissionType permissionType,
        string? url = null,
        string? icon = null,
        int? sortOrder = null)
    {
        if (string.IsNullOrWhiteSpace(permissionName))
            throw new ArgumentException("Permission name cannot be null or empty", nameof(permissionName));

        PermissionName = permissionName.Trim();
        PermissionType = permissionType;
        if (url is not null) Url = url.Length == 0 ? null : url;
        if (icon is not null) Icon = icon.Length == 0 ? null : icon;
        if (sortOrder.HasValue) SortOrder = sortOrder.Value;
    }

    /// <summary>
    /// 变更父节点（null = 移至根；禁止挂到自己名下；深层成环由调用方沿父链校验）
    /// </summary>
    public void ChangeParent(Guid? parentId)
    {
        if (parentId == PermissionId)
            throw new ArgumentException("不能将权限挂到自己名下", nameof(parentId));
        ParentId = parentId;
    }

    /// <summary>
    /// 软删除（含自身语义：删除目录前调用方应先处理其子节点）
    /// </summary>
    public void SoftDelete(bool deleted)
    {
        IsDeleted = deleted;
    }

    public bool IsMenuPermission() => PermissionType == PermissionType.Menu;

    public bool IsApiPermission() => PermissionType == PermissionType.Api;

    public bool IsRoot => ParentId is null;
}

namespace Identity.Domain.Entities.MenuAggregate;

/// <summary>
/// 菜单实体（树状结构，聚合根）
///
/// 用于管理导航菜单：节点经 ParentId 自引用构成菜单树。
/// - 目录（MenuType.Directory）：模块分组，可含子菜单，通常无路由；
/// - 菜单项（MenuType.Item）：可点击跳转的页面入口，携带 Url。
///
/// 可见性绑定：RequiredPermissionCode（按权限编码前缀段匹配）与 RequiredRole（角色名集合），
/// 二者均为空表示对全部用户可见；均有值时为「与」关系。
/// </summary>
public class Menu : Entity<Guid>, IAggregateRoot
{
    protected Menu()
    {
        MenuId = Guid.CreateVersion7();
        Children = new List<Menu>();
        IsEnabled = true;
        IsDeleted = false;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 创建菜单节点
    /// </summary>
    /// <param name="menuName">菜单名称</param>
    /// <param name="menuType">菜单类型</param>
    /// <param name="parentId">父节点 ID（null = 根节点）</param>
    /// <param name="url">路由路径（目录可空）</param>
    /// <param name="icon">图标键（前端图标注册表的键）</param>
    /// <param name="sortOrder">排序号（同级内升序）</param>
    /// <param name="requiredPermissionCode">可见所需权限编码（null = 不限制）</param>
    /// <param name="requiredRole">可见所需角色名（逗号分隔，null = 不限制）</param>
    public Menu(
        string menuName,
        MenuType menuType,
        Guid? parentId = null,
        string? url = null,
        string? icon = null,
        int sortOrder = 0,
        string? requiredPermissionCode = null,
        string? requiredRole = null) : this()
    {
        if (string.IsNullOrWhiteSpace(menuName))
            throw new ArgumentException("Menu name cannot be null or empty", nameof(menuName));

        if (parentId == MenuId)
            throw new ArgumentException("不能将菜单挂到自己名下", nameof(parentId));

        MenuName = menuName.Trim();
        MenuType = menuType;
        ParentId = parentId;
        Url = string.IsNullOrWhiteSpace(url) ? null : url.Trim();
        Icon = string.IsNullOrWhiteSpace(icon) ? null : icon.Trim();
        SortOrder = sortOrder;
        RequiredPermissionCode = string.IsNullOrWhiteSpace(requiredPermissionCode) ? null : requiredPermissionCode.Trim();
        RequiredRole = string.IsNullOrWhiteSpace(requiredRole) ? null : requiredRole.Trim();
    }

    /// <summary>
    /// 菜单ID
    /// </summary>
    public Guid MenuId { get; init; }

    /// <summary>
    /// 菜单名称
    /// </summary>
    public string MenuName { get; private set; } = null!;

    /// <summary>
    /// 菜单类型（目录/菜单项）
    /// </summary>
    public MenuType MenuType { get; private set; }

    /// <summary>
    /// 父节点 ID（null = 根节点）
    /// </summary>
    public Guid? ParentId { get; private set; }

    /// <summary>
    /// 路由路径（目录可空）
    /// </summary>
    public string? Url { get; private set; }

    /// <summary>
    /// 图标键（可空）
    /// </summary>
    public string? Icon { get; private set; }

    /// <summary>
    /// 同级排序号（升序）
    /// </summary>
    public int SortOrder { get; private set; }

    /// <summary>
    /// 是否启用（禁用后不参与导航渲染）
    /// </summary>
    public bool IsEnabled { get; private set; }

    /// <summary>
    /// 可见所需权限编码（可空；非空时按前缀段匹配判定）
    /// </summary>
    public string? RequiredPermissionCode { get; private set; }

    /// <summary>
    /// 可见所需角色名（可空；逗号分隔，与用户角色名匹配）
    /// </summary>
    public string? RequiredRole { get; private set; }

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
    public Menu? Parent { get; private set; }

    /// <summary>
    /// 子节点集合
    /// </summary>
    public ICollection<Menu> Children { get; private set; }

    /// <summary>
    /// 更新基本信息（MenuName/MenuType 必填；Url/Icon/RequiredPermissionCode/RequiredRole
    /// 传 null 表示不修改，传空串表示清除；SortOrder 传 null 表示不修改）
    /// </summary>
    public void ChangeMenu(
        string menuName,
        MenuType menuType,
        string? url = null,
        string? icon = null,
        int? sortOrder = null,
        string? requiredPermissionCode = null,
        string? requiredRole = null)
    {
        if (string.IsNullOrWhiteSpace(menuName))
            throw new ArgumentException("Menu name cannot be null or empty", nameof(menuName));

        MenuName = menuName.Trim();
        MenuType = menuType;
        if (url is not null) Url = url.Length == 0 ? null : url.Trim();
        if (icon is not null) Icon = icon.Length == 0 ? null : icon.Trim();
        if (sortOrder.HasValue) SortOrder = sortOrder.Value;
        if (requiredPermissionCode is not null)
            RequiredPermissionCode = requiredPermissionCode.Length == 0 ? null : requiredPermissionCode.Trim();
        if (requiredRole is not null)
            RequiredRole = requiredRole.Length == 0 ? null : requiredRole.Trim();
    }

    /// <summary>
    /// 变更父节点（null = 移至根；禁止挂到自己名下；深层成环由调用方沿父链校验）
    /// </summary>
    public void ChangeParent(Guid? parentId)
    {
        if (parentId == MenuId)
            throw new ArgumentException("不能将菜单挂到自己名下", nameof(parentId));
        ParentId = parentId;
    }

    /// <summary>
    /// 启用
    /// </summary>
    public void Enable() => IsEnabled = true;

    /// <summary>
    /// 禁用
    /// </summary>
    public void Disable() => IsEnabled = false;

    /// <summary>
    /// 软删除（删除目录前调用方应先处理其子节点）
    /// </summary>
    public void SoftDelete(bool deleted) => IsDeleted = deleted;

    public bool IsDirectory() => MenuType == MenuType.Directory;

    public bool IsItem() => MenuType == MenuType.Item;

    public bool IsRoot => ParentId is null;
}
namespace Identity.Web.API.Application.Commands;

// ─── Menu Commands ───

public record CreateMenuCommand(
    string MenuName,
    MenuType MenuType,
    Guid? ParentId = null,
    string? Url = null,
    string? Icon = null,
    int SortOrder = 0,
    string? RequiredPermissionCode = null,
    string? RequiredRole = null
) : IRequest<CreateMenuResult>, ILoggableCommand
{
    public string IdProperty => nameof(MenuName);
    public string IdValue => MenuName;
}

public record CreateMenuResult(Guid MenuId, string MenuName);

/// <summary>
/// 更新菜单。语义：MenuName/MenuType 传 null 表示不修改；
/// Url/Icon/RequiredPermissionCode/RequiredRole 传 null 表示不修改（传空串表示清除）；
/// SortOrder/IsEnabled 传 null 表示不修改；ParentId 传 null 表示不修改父节点，Guid.Empty 表示移至根。
/// </summary>
public record UpdateMenuCommand(
    Guid MenuId,
    string? MenuName = null,
    MenuType? MenuType = null,
    Guid? ParentId = null,
    string? Url = null,
    string? Icon = null,
    int? SortOrder = null,
    bool? IsEnabled = null,
    string? RequiredPermissionCode = null,
    string? RequiredRole = null
) : IRequest<bool>, ILoggableCommand
{
    public string IdProperty => nameof(MenuId);
    public string IdValue => MenuId.ToString();
}

public record DeleteMenuCommand(Guid MenuId)
    : IRequest<bool>, ILoggableCommand
{
    public string IdProperty => nameof(MenuId);
    public string IdValue => MenuId.ToString();
}
using Identity.Web.API.Application.Commands;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Web.API.APIs;

/// <summary>
/// 菜单管理路由映射 API
///
/// 提供菜单 CRUD、管理端完整菜单树查询，以及当前用户可见菜单树查询（供管理端侧栏渲染）。
/// 可见性绑定：RequiredPermissionCode（前缀段匹配）与 RequiredRole（角色名），二者为「与」关系。
/// </summary>
public static class MenuApi
{
    public static RouteGroupBuilder MapMenuApi(this RouteGroupBuilder routeBuilder)
    {
        var route = routeBuilder
            .MapGroup("/menu")
            .WithHttpLogging(HttpLoggingFields.All);

        // ── 菜单 CRUD ──
        route.MapPost(string.Empty, CreateMenuAsync)
            .RequirePermission("api:identity:manage")
            .RequireAuthorization("AdminOnly")
            .WithDescription("创建菜单")
            .Produces<CreateMenuResult>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        route.MapPut("/{menuId:guid}", UpdateMenuAsync)
            .RequirePermission("api:identity:manage")
            .RequireAuthorization("AdminOnly")
            .WithDescription("更新菜单")
            .Produces<bool>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        route.MapDelete("/{menuId:guid}", DeleteMenuAsync)
            .RequirePermission("api:identity:manage")
            .RequireAuthorization("AdminOnly")
            .WithDescription("删除菜单（软删除）")
            .Produces<bool>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        // ── 菜单树查询 ──
        route.MapGet("/tree", GetMenuTreeAsync)
            .RequirePermission("api:identity:manage")
            .RequireAuthorization("AdminOnly")
            .WithDescription("完整菜单树查询（管理端渲染/维护用）");

        // 当前用户可见菜单树（启用 + 权限/角色命中；管理端侧栏渲染用，仅需认证）
        route.MapGet("/my", GetMyMenusAsync)
            .RequireAuthorization()
            .WithDescription("当前用户可见菜单树");

        return route;
    }

    // ──────────── 菜单 CRUD 端点实现 ────────────

    /// <summary>
    /// POST /api/identity/menu — 创建菜单
    /// </summary>
    private static async Task<IResult> CreateMenuAsync(
        [FromServices] INotMediator mediator,
        [FromBody] CreateMenuCommand command,
        HttpContext httpContext)
    {
        try
        {
            var identityCommand = new IdentifiedCommand<CreateMenuCommand, CreateMenuResult>(
                IdentityApiHelpers.GetIdempotencyKey(httpContext), command);
            var result = await mediator.SendAsync(identityCommand);

            return string.IsNullOrEmpty(result.MenuName)
                ? Results.Problem("创建失败，请重试", statusCode: StatusCodes.Status500InternalServerError)
                : Results.Created($"/api/identity/menu/{result.MenuId}", result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// PUT /api/identity/menu/{menuId} — 更新菜单
    /// </summary>
    private static async Task<IResult> UpdateMenuAsync(
        [FromServices] INotMediator mediator,
        [FromRoute] Guid menuId,
        [FromBody] UpdateMenuCommand update,
        HttpContext httpContext)
    {
        try
        {
            var command = update with { MenuId = menuId };
            var identityCommand = new IdentifiedCommand<UpdateMenuCommand, bool>(
                IdentityApiHelpers.GetIdempotencyKey(httpContext), command);
            var result = await mediator.SendAsync(identityCommand);
            return Results.Ok(new { success = result });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// DELETE /api/identity/menu/{menuId} — 删除菜单（软删除）
    /// </summary>
    private static async Task<IResult> DeleteMenuAsync(
        [FromServices] INotMediator mediator,
        [FromRoute] Guid menuId,
        HttpContext httpContext)
    {
        try
        {
            var command = new DeleteMenuCommand(menuId);
            var identityCommand = new IdentifiedCommand<DeleteMenuCommand, bool>(
                IdentityApiHelpers.GetIdempotencyKey(httpContext), command);
            var result = await mediator.SendAsync(identityCommand);
            return Results.Ok(new { success = result });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    // ──────────── 菜单树查询端点实现 ────────────

    /// <summary>
    /// GET /api/identity/menu/tree — 完整菜单树（未删除节点组装成的森林）
    /// </summary>
    private static async Task<IResult> GetMenuTreeAsync(
        [FromServices] IMenuRepository menuRepository,
        CancellationToken ct)
    {
        var all = await menuRepository.GetAllAsync(ct);
        return Results.Ok(BuildTree(all));
    }

    /// <summary>
    /// GET /api/identity/menu/my — 当前用户可见菜单树（启用 + 权限/角色命中）
    /// </summary>
    private static async Task<IResult> GetMyMenusAsync(
        [FromServices] IMenuRepository menuRepository,
        [FromServices] IPermissionChecker permissionChecker,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return Results.Unauthorized();

        var permissions = await permissionChecker.GetUserPermissionsAsync(userId, ct);
        var roles = httpContext.User.FindAll(ClaimTypes.Role)
            .SelectMany(c => c.Value.Split(',',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var all = await menuRepository.GetAllAsync(ct);
        var visible = all.Where(m => m.IsEnabled && IsVisible(m, permissions, roles));
        return Results.Ok(BuildTree(visible));
    }

    /// <summary>
    /// 可见性判定：权限门槛（前缀段匹配，与 PermissionChecker 同规则）与角色门槛（角色名交集），均为「与」。
    /// </summary>
    private static bool IsVisible(Menu menu, IReadOnlySet<string> permissions, IReadOnlySet<string> roles)
    {
        // 权限门槛：授权集合含目标码自身，或含其某个祖先段码（目录授权覆盖子孙）
        if (!string.IsNullOrWhiteSpace(menu.RequiredPermissionCode))
        {
            var code = menu.RequiredPermissionCode;
            var hit = permissions.Any(granted =>
                string.Equals(code, granted, StringComparison.OrdinalIgnoreCase)
                || code.StartsWith(granted + ":", StringComparison.OrdinalIgnoreCase));
            if (!hit) return false;
        }

        // 角色门槛：节点要求角色名与用户角色名有交集
        if (!string.IsNullOrWhiteSpace(menu.RequiredRole))
        {
            var required = menu.RequiredRole.Split(',',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (!required.Any(roles.Contains)) return false;
        }

        return true;
    }

    /// <summary>
    /// 把菜单节点组装成森林（根节点列表，children 嵌套）；
    /// 父缺失/已删除的节点提升为根，防悬挂节点不可达。输入须已按 SortOrder 排序。
    /// </summary>
    private static List<MenuTreeNodeDto> BuildTree(IEnumerable<Menu> menus)
    {
        var list = menus.ToList();
        if (list.Count == 0)
            return new List<MenuTreeNodeDto>();

        // 第一遍：浅层 DTO 字典（children 待挂接）
        var nodes = new Dictionary<Guid, MenuTreeNodeDto>(list.Count);
        foreach (var m in list)
            nodes[m.MenuId] = new MenuTreeNodeDto(
                m.MenuId,
                m.ParentId,
                m.MenuName,
                m.MenuType,
                m.Url,
                m.Icon,
                m.SortOrder,
                m.IsEnabled,
                m.RequiredPermissionCode,
                m.RequiredRole);

        // 第二遍：按 SortOrder 顺序把子节点挂到父的 Children
        var roots = new List<MenuTreeNodeDto>();
        foreach (var m in list)
        {
            var node = nodes[m.MenuId];
            if (m.ParentId is { } parentId && nodes.TryGetValue(parentId, out var parent))
                parent.Children.Add(node);
            else
                roots.Add(node);
        }

        return roots;
    }
}

/// <summary>
/// 菜单树节点 DTO（children 递归嵌套；MenuType 序列化为数字 1/2）
/// </summary>
public sealed record MenuTreeNodeDto(
    Guid MenuId,
    Guid? ParentId,
    string MenuName,
    MenuType MenuType,
    string? Url,
    string? Icon,
    int SortOrder,
    bool IsEnabled,
    string? RequiredPermissionCode,
    string? RequiredRole)
{
    public List<MenuTreeNodeDto> Children { get; } = new();
}
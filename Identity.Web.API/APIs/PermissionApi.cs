using Identity.Web.API.Application.Commands;
using Identity.Domain.IService;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Web.API.APIs;

/// <summary>
/// 权限路由映射 API
///
/// 提供权限 CRUD 操作及 URL→PermissionCode 映射表查询。
/// </summary>
public static class PermissionApi
{
    public static RouteGroupBuilder MapPermissionApi(this RouteGroupBuilder routeBuilder)
    {
        var route = routeBuilder.MapGroup("/permission");

        // ── 权限 CRUD ──
        route.MapPost(string.Empty, CreatePermissionAsync)
            .RequirePermission("api:identity:manage")
            .RequireAuthorization("AdminOnly")
            .WithHttpLogging(HttpLoggingFields.All)
            .WithDescription("创建权限")
            .Produces<CreatePermissionResult>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        route.MapPut("/{permissionId:guid}", UpdatePermissionAsync)
            .RequirePermission("api:identity:manage")
            .RequireAuthorization("AdminOnly")
            .WithHttpLogging(HttpLoggingFields.All)
            .WithDescription("更新权限")
            .Produces<bool>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        route.MapDelete("/{permissionId:guid}", DeletePermissionAsync)
            .RequirePermission("api:identity:manage")
            .RequireAuthorization("AdminOnly")
            .WithHttpLogging(HttpLoggingFields.All)
            .WithDescription("删除权限（软删除）")
            .Produces<bool>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

                // ── 权限树查询（管理端渲染/勾选用；96699123 遗留 handler 未挂载，本次下沉补挂）──
        route.MapGet("/tree", GetPermissionTreeAsync)
            .RequireAuthorization("AdminOnly")
            .WithDescription("权限树查询（管理端渲染/勾选用）");

return route;
    }

    // ──────────── 权限 CRUD 端点实现 ────────────

    /// <summary>
    /// POST /api/identity/permission — 创建权限
    /// </summary>
    private static async Task<IResult> CreatePermissionAsync(
        [FromServices] INotMediator mediator,
        [FromBody] CreatePermissionCommand command,
        HttpContext httpContext)
    {
        try
        {
            var identityCommand = new IdentifiedCommand<CreatePermissionCommand, CreatePermissionResult>(
                IdentityApiHelpers.GetIdempotencyKey(httpContext), command);
            var result = await mediator.SendAsync(identityCommand);

            return string.IsNullOrEmpty(result.PermissionCode)
                ? Results.Problem("创建失败，请重试", statusCode: StatusCodes.Status500InternalServerError)
                : Results.Created($"/api/identity/permission/{result.PermissionId}", result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// PUT /api/identity/permission/{permissionId} — 更新权限
    /// </summary>
    private static async Task<IResult> UpdatePermissionAsync(
        [FromServices] INotMediator mediator,
        [FromRoute] Guid permissionId,
        [FromBody] UpdatePermissionCommand update,
        HttpContext httpContext)
    {
        try
        {
            var command = update with { PermissionId = permissionId };
            var identityCommand = new IdentifiedCommand<UpdatePermissionCommand, bool>(
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
    /// DELETE /api/identity/permission/{permissionId} — 删除权限（软删除）
    /// </summary>
    private static async Task<IResult> DeletePermissionAsync(
        [FromServices] INotMediator mediator,
        [FromRoute] Guid permissionId,
        HttpContext httpContext)
    {
        try
        {
            var command = new DeletePermissionCommand(permissionId);
            var identityCommand = new IdentifiedCommand<DeletePermissionCommand, bool>(
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
    /// GET /api/identity/permission/tree — 权限树查询（管理端渲染/勾选用）
    /// 返回未删除节点组装成的森林（根节点列表，children 嵌套）。
    /// </summary>
    private static async Task<IResult> GetPermissionTreeAsync(
        [FromServices] IPermissionRepository permissionRepository,
        CancellationToken ct)
    {
        var all = await permissionRepository.GetAllAsync(ct);
        if (all.Count == 0)
            return Results.Ok(new List<PermissionTreeNodeDto>());

        // 第一遍：浅层 DTO 字典（children 待挂接）
        var nodes = new Dictionary<Guid, PermissionTreeNodeDto>(all.Count);
        foreach (var p in all)
            nodes[p.PermissionId] = new PermissionTreeNodeDto(
                p.PermissionId,
                p.ParentId,
                p.PermissionCode,
                p.PermissionName,
                p.PermissionType,
                p.Url,
                p.Icon,
                p.SortOrder);

        // 第二遍：按 SortOrder 顺序（GetAllAsync 已排序）把子节点挂到父的 Children
        var roots = new List<PermissionTreeNodeDto>();
        foreach (var p in all)
        {
            var node = nodes[p.PermissionId];
            if (p.ParentId is { } parentId && nodes.TryGetValue(parentId, out var parent))
                parent.Children.Add(node);
            else
                roots.Add(node); // 父缺失/已删除 → 提升为根（防悬挂节点不可达）
        }

        return Results.Ok(roots);
    }
}

/// <summary>
/// 权限树节点 DTO（children 递归嵌套；PermissionType 序列化为数字 1/2/3）
/// </summary>
public sealed record PermissionTreeNodeDto(
    Guid PermissionId,
    Guid? ParentId,
    string PermissionCode,
    string PermissionName,
    PermissionType PermissionType,
    string? Url,
    string? Icon,
    int SortOrder)
{
    public List<PermissionTreeNodeDto> Children { get; } = new();
}

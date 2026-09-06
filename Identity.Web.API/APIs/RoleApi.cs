using Identity.Web.API.Application.Commands;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Web.API.APIs;

public static class RoleApi
{
    public static RouteGroupBuilder MapRoleApi(this RouteGroupBuilder routeBuilder)
    {
        var route = routeBuilder
            .MapGroup("/role")
            .WithHttpLogging(HttpLoggingFields.All);

        route.MapPost(string.Empty, CreateRoleAsync)
            .RequireAuthorization("AdminOnly")
            .WithDescription("创建角色")
            .Produces<CreateRoleResult>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        route.MapPut("/{roleId:guid}", UpdateRoleAsync)
            .RequireAuthorization("AdminOnly")
            .WithDescription("更新角色")
            .Produces<bool>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        route.MapDelete("/{roleId:guid}", DeleteRoleAsync)
            .RequireAuthorization("AdminOnly")
            .WithDescription("删除角色（软删除）")
            .Produces<bool>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        // ── 角色-权限分配（树形授权；PUT 全量覆盖，可含目录码，授权目录=放行全部子孙）──
        route.MapGet("/{roleId:guid}/permissions", GetRolePermissionsAsync)
            .RequireAuthorization("AdminOnly")
            .WithDescription("获取角色当前已授权权限 ID 列表（管理端树形勾选回显）")
            .Produces<List<RolePermissionItemDto>>(StatusCodes.Status200OK);

        route.MapPut("/{roleId:guid}/permissions", SetRolePermissionsAsync)
            .RequireAuthorization("AdminOnly")
            .WithDescription("全量覆盖角色权限（permissionIds 可含目录码；保存后吊销持该角色用户的会话）")
            .Produces<bool>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return route;
    }

    private static async Task<IResult> CreateRoleAsync(
        [FromServices] INotMediator mediator,
        [FromBody] CreateRoleCommand command,
        HttpContext httpContext)
    {
        try
        {
            var identityCommand = new IdentifiedCommand<CreateRoleCommand, CreateRoleResult>(
                IdentityApis.GetIdempotencyKey(httpContext), command);
            var result = await mediator.SendAsync(identityCommand);

            return string.IsNullOrEmpty(result.RoleName)
                ? Results.Problem("创建失败，请重试", statusCode: 500)
                : Results.Created($"/api/identity/role/{result.RoleGuid}", result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> UpdateRoleAsync(
        [FromServices] INotMediator mediator,
        [FromRoute] Guid roleId,
        [FromBody] UpdateRoleCommand update,
        HttpContext httpContext)
    {
        try
        {
            var command = update with { RoleGuid = roleId };
            var identityCommand = new IdentifiedCommand<UpdateRoleCommand, bool>(
                IdentityApis.GetIdempotencyKey(httpContext), command);
            var result = await mediator.SendAsync(identityCommand);
            return Results.Ok(new { success = result });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> DeleteRoleAsync(
        [FromServices] INotMediator mediator,
        [FromRoute] Guid roleId,
        HttpContext httpContext)
    {
        try
        {
            var command = new DeleteRoleCommand(roleId);
            var identityCommand = new IdentifiedCommand<DeleteRoleCommand, bool>(
                IdentityApis.GetIdempotencyKey(httpContext), command);
            var result = await mediator.SendAsync(identityCommand);
            return Results.Ok(new { success = result });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// GET /api/identity/role/{roleId}/permissions — 角色已授权权限列表（树形勾选回显用）
    /// </summary>
    private static async Task<IResult> GetRolePermissionsAsync(
        [FromServices] IUserRoleRepository userRoleRepository,
        [FromRoute] Guid roleId,
        CancellationToken ct)
    {
        var role = await userRoleRepository.FindByUserRoleWithPermissionsAsync(roleId);
        if (role is null || role.IsDeleted)
            return Results.NotFound(new { error = "角色不存在" });

        var items = role.Permissions
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.PermissionCode)
            .Select(p => new RolePermissionItemDto(
                p.PermissionId, p.PermissionCode, p.PermissionName, p.PermissionType))
            .ToList();

        return Results.Ok(items);
    }

    /// <summary>
    /// PUT /api/identity/role/{roleId}/permissions — 全量覆盖角色权限（树形授权）
    /// body: { permissionIds: Guid[] }；目录码入库即覆盖其全部子孙；保存后吊销持该角色用户会话。
    /// </summary>
    private static async Task<IResult> SetRolePermissionsAsync(
        [FromServices] INotMediator mediator,
        [FromRoute] Guid roleId,
        [FromBody] SetRolePermissionsRequest request,
        HttpContext httpContext)
    {
        try
        {
            var command = new SetRolePermissionsCommand(roleId, request.PermissionIds ?? []);
            var identityCommand = new IdentifiedCommand<SetRolePermissionsCommand, bool>(
                IdentityApis.GetIdempotencyKey(httpContext), command);
            var result = await mediator.SendAsync(identityCommand);
            return Results.Ok(new { success = result });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }
}

/// <summary>
/// 角色权限分配请求体
/// </summary>
public sealed record SetRolePermissionsRequest
{
    /// <summary>权限 ID 集合（可含目录码；空数组 = 清空角色全部权限）</summary>
    public List<Guid>? PermissionIds { get; init; }
}

/// <summary>
/// 角色已授权权限条目 DTO
/// </summary>
public sealed record RolePermissionItemDto(
    Guid PermissionId,
    string PermissionCode,
    string PermissionName,
    PermissionType PermissionType);

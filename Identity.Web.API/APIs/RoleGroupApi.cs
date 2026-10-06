using Identity.Web.API.Application.Commands;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Web.API.APIs;

public static class RoleGroupApi
{
    public static RouteGroupBuilder MapRoleGroupApi(this RouteGroupBuilder routeBuilder)
    {
        var route = routeBuilder
            .MapGroup("/rolegroup")
            .WithHttpLogging(HttpLoggingFields.All);

        route.MapPost(string.Empty, CreateRoleGroupAsync)
            .RequirePermission("api:identity:manage")
            .RequireAuthorization("AdminOnly")
            .WithDescription("创建角色组")
            .Produces<CreateRoleGroupResult>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        route.MapGet(string.Empty, GetRoleGroupsAsync)
            .RequirePermission("api:identity:read")
            .RequireAuthorization("AdminOnly")
            .WithDescription("获取全部未删除角色组列表（含已授权限数与关联角色数）")
            .Produces<List<RoleGroupListItemDto>>(StatusCodes.Status200OK);

        route.MapPut("/{groupId:guid}", UpdateRoleGroupAsync)
            .RequirePermission("api:identity:manage")
            .RequireAuthorization("AdminOnly")
            .WithDescription("更新角色组")
            .Produces<bool>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        route.MapDelete("/{groupId:guid}", DeleteRoleGroupAsync)
            .RequirePermission("api:identity:manage")
            .RequireAuthorization("AdminOnly")
            .WithDescription("删除角色组（软删除）")
            .Produces<bool>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        // ── 角色组-权限分配（树形授权；PUT 全量覆盖，可含目录码，授权目录=放行全部子孙）──
        route.MapGet("/{groupId:guid}/permissions", GetRoleGroupPermissionsAsync)
            .RequirePermission("api:identity:read")
            .RequireAuthorization("AdminOnly")
            .WithDescription("获取角色组当前已授权权限列表（管理端树形勾选回显）")
            .Produces<List<RolePermissionItemDto>>(StatusCodes.Status200OK);

        route.MapPut("/{groupId:guid}/permissions", SetRoleGroupPermissionsAsync)
            .RequirePermission("api:identity:manage")
            .RequireAuthorization("AdminOnly")
            .WithDescription("全量覆盖角色组权限（permissionIds 可含目录码）")
            .Produces<bool>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return route;
    }

    private static async Task<IResult> CreateRoleGroupAsync(
        [FromServices] INotMediator mediator,
        [FromBody] CreateRoleGroupCommand command,
        HttpContext httpContext)
    {
        try
        {
            var identityCommand = new IdentifiedCommand<CreateRoleGroupCommand, CreateRoleGroupResult>(
                IdentityApiHelpers.GetIdempotencyKey(httpContext), command);
            var result = await mediator.SendAsync(identityCommand);

            return string.IsNullOrEmpty(result.RoleGroupName)
                ? Results.Problem("创建失败，请重试", statusCode: 500)
                : Results.Created($"/api/identity/rolegroup/{result.RoleGroupGuid}", result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> UpdateRoleGroupAsync(
        [FromServices] INotMediator mediator,
        [FromRoute] Guid groupId,
        [FromBody] UpdateRoleGroupCommand update,
        HttpContext httpContext)
    {
        try
        {
            var command = update with { RoleGroupGuid = groupId };
            var identityCommand = new IdentifiedCommand<UpdateRoleGroupCommand, bool>(
                IdentityApiHelpers.GetIdempotencyKey(httpContext), command);
            var result = await mediator.SendAsync(identityCommand);
            return Results.Ok(new { success = result });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> DeleteRoleGroupAsync(
        [FromServices] INotMediator mediator,
        [FromRoute] Guid groupId,
        HttpContext httpContext)
    {
        try
        {
            var command = new DeleteRoleGroupCommand(groupId);
            var identityCommand = new IdentifiedCommand<DeleteRoleGroupCommand, bool>(
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
    /// GET /api/identity/rolegroup — 全部未删除角色组列表（管理端角色组列表用，含已授权限数与关联角色数）
    /// </summary>
    private static async Task<IResult> GetRoleGroupsAsync(
        [FromServices] IRoleGroupRepository roleGroupRepository,
        CancellationToken ct)
    {
        var groups = await roleGroupRepository.GetAllWithPermissionsAsync();
        var items = groups
            .Where(g => !g.IsDeleted)
            .OrderBy(g => g.RoleGroupCode)
            .Select(g => new RoleGroupListItemDto(
                g.RoleGroupGuid, g.RoleGroupName, g.RoleGroupCode,
                g.Permissions.Count(p => !p.IsDeleted), g.RoleGuids.Count))
            .ToList();

        return Results.Ok(items);
    }

    /// <summary>
    /// GET /api/identity/rolegroup/{groupId}/permissions — 角色组已授权权限列表（树形勾选回显用）
    /// </summary>
    private static async Task<IResult> GetRoleGroupPermissionsAsync(
        [FromServices] IRoleGroupRepository roleGroupRepository,
        [FromRoute] Guid groupId,
        CancellationToken ct)
    {
        var roleGroup = await roleGroupRepository.FindByRoleGroupWithPermissionsAsync(groupId);
        if (roleGroup is null || roleGroup.IsDeleted)
            return Results.NotFound(new { error = "角色组不存在" });

        var items = roleGroup.Permissions
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.PermissionCode)
            .Select(p => new RolePermissionItemDto(
                p.PermissionId, p.PermissionCode, p.PermissionName, p.PermissionType))
            .ToList();

        return Results.Ok(items);
    }

    /// <summary>
    /// PUT /api/identity/rolegroup/{groupId}/permissions — 全量覆盖角色组权限（树形授权）
    /// body: { permissionIds: Guid[] }；目录码入库即覆盖其全部子孙。
    /// </summary>
    private static async Task<IResult> SetRoleGroupPermissionsAsync(
        [FromServices] INotMediator mediator,
        [FromRoute] Guid groupId,
        [FromBody] SetRolePermissionsRequest request,
        HttpContext httpContext)
    {
        try
        {
            var command = new SetRoleGroupPermissionsCommand(groupId, request.PermissionIds ?? []);
            var identityCommand = new IdentifiedCommand<SetRoleGroupPermissionsCommand, bool>(
                IdentityApiHelpers.GetIdempotencyKey(httpContext), command);
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
/// 角色组列表条目 DTO
/// </summary>
public sealed record RoleGroupListItemDto(
    Guid RoleGroupGuid,
    string RoleGroupName,
    string RoleGroupCode,
    int PermissionCount,
    int RoleCount);

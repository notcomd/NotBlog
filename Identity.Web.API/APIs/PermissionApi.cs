using Identity.Web.API.Application.Commands;
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

        // ── 网关映射查询（启动时高频调用）──
        route.MapGet("/mappings", GetMappings)
            .WithHttpLogging(HttpLoggingFields.None);

        // ── 权限 CRUD ──
        route.MapPost(string.Empty, CreatePermissionAsync)
            .RequireAuthorization("AdminOnly")
            .WithHttpLogging(HttpLoggingFields.All)
            .WithDescription("创建权限")
            .Produces<CreatePermissionResult>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        route.MapPut("/{permissionId:guid}", UpdatePermissionAsync)
            .RequireAuthorization("AdminOnly")
            .WithHttpLogging(HttpLoggingFields.All)
            .WithDescription("更新权限")
            .Produces<bool>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        route.MapDelete("/{permissionId:guid}", DeletePermissionAsync)
            .RequireAuthorization("AdminOnly")
            .WithHttpLogging(HttpLoggingFields.All)
            .WithDescription("删除权限（软删除）")
            .Produces<bool>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

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
                IdentityApis.GetIdempotencyKey(httpContext), command);
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
                IdentityApis.GetIdempotencyKey(httpContext), command);
            var result = await mediator.SendAsync(identityCommand);
            return Results.Ok(new { success = result });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    // ──────────── 网关映射查询（已有）────────────

    /// <summary>
    /// GET /api/identity/permission/mappings
    ///
    /// 返回全部 URL→PermissionCode 映射，供网关启动时加载路由表。
    /// 响应格式与 NotBlog_Yarp 的 PermissionOptions.Mappings 完全兼容。
    /// </summary>
    private static IResult GetMappings([FromServices] IConfiguration configuration)
    {
        var mappings = configuration
            .GetSection("PermissionMappings")
            .Get<List<PermissionMappingDto>>();

        if (mappings is null || mappings.Count == 0)
            return Results.Ok(Array.Empty<PermissionMappingDto>());

        return Results.Ok(mappings);
    }
}

/// <summary>
/// 权限路由映射 DTO — 与 NotBlog_Yarp 侧的 RouteMapping 结构一致
/// </summary>
public sealed record PermissionMappingDto
{
    public string Method { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
}

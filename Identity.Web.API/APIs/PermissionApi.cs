using Identity.Web.API.Application.Commands;
using Identity.Domain.IService;
using Identity.Web.API.Filters;
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

        // ── 网关映射查询（启动时高频调用，V4：需 X-Internal-Api-Key）──
        route.MapGet("/mappings", GetMappings)
            .AddEndpointFilter<InternalApiKeyFilter>()
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

        // ── 网关权限检查端点（供 NotBlog_Yarp 网关每请求调用，V4：全部要求 X-Internal-Api-Key）──
        // 仅暴露「是否有权限」布尔与数据范围，不暴露业务数据；密钥由 GatewayInternal:ApiKey /
        // 环境变量 GATEWAY_INTERNAL_API_KEY 配置，未配置或错误一律 401（fail-closed）。
        route.MapPost("/check-and-scope", CheckAndGetScopeAsync)
            .AddEndpointFilter<InternalApiKeyFilter>()
            .WithHttpLogging(HttpLoggingFields.None)
            .WithDescription("权限检查 + 数据范围组合查询（网关内部调用，需 X-Internal-Api-Key）");

        route.MapPost("/check", CheckPermissionAsync)
            .AddEndpointFilter<InternalApiKeyFilter>()
            .WithHttpLogging(HttpLoggingFields.None)
            .WithDescription("权限检查（网关内部调用，需 X-Internal-Api-Key）");

        route.MapGet("/datascope/{userId:guid}", GetDataScopeAsync)
            .AddEndpointFilter<InternalApiKeyFilter>()
            .WithHttpLogging(HttpLoggingFields.None)
            .WithDescription("获取用户数据范围（网关内部调用，需 X-Internal-Api-Key）");

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

    // ──────────── 网关权限检查端点实现 ────────────

    /// <summary>
    /// POST /api/identity/permission/check-and-scope — 权限检查 + 数据范围组合查询
    /// 响应格式与网关 HttpPermissionServiceClient.CombinedResult 匹配：
    /// { hasPermission, dataScope }（dataScope 为 "type|value1,value2,..." 格式）
    /// </summary>
    private static async Task<IResult> CheckAndGetScopeAsync(
        [FromServices] IPermissionChecker checker,
        [FromBody] PermissionCheckRequest request,
        CancellationToken ct)
    {
        try
        {
            if (request.UserId == Guid.Empty || string.IsNullOrWhiteSpace(request.PermissionCode))
                return Results.BadRequest(new { error = "userId 与 permissionCode 不能为空" });

            var hasPermission = await checker.CheckPermissionAsync(request.UserId, request.PermissionCode, ct);
            if (!hasPermission)
                return Results.Ok(new { hasPermission = false, dataScope = "0|" });

            var scope = await checker.GetUserDataScopeAsync(request.UserId, ct);
            return Results.Ok(new { hasPermission = true, dataScope = scope.ToClaimValue() });
        }
        catch (Exception)
        {
            return Results.Problem("权限检查失败", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// POST /api/identity/permission/check — 权限检查
    /// 响应格式与网关 HttpPermissionServiceClient.CheckResult 匹配：{ hasPermission }
    /// </summary>
    private static async Task<IResult> CheckPermissionAsync(
        [FromServices] IPermissionChecker checker,
        [FromBody] PermissionCheckRequest request,
        CancellationToken ct)
    {
        try
        {
            if (request.UserId == Guid.Empty || string.IsNullOrWhiteSpace(request.PermissionCode))
                return Results.BadRequest(new { error = "userId 与 permissionCode 不能为空" });

            var hasPermission = await checker.CheckPermissionAsync(request.UserId, request.PermissionCode, ct);
            return Results.Ok(new { hasPermission });
        }
        catch (Exception)
        {
            return Results.Problem("权限检查失败", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// GET /api/identity/permission/datascope/{userId} — 获取用户数据范围
    /// 响应格式与网关 HttpPermissionServiceClient.DataScopeResult 匹配：
    /// { scopeType, values }（scopeType: 0=Own 1=Department 2=All）
    /// </summary>
    private static async Task<IResult> GetDataScopeAsync(
        [FromServices] IPermissionChecker checker,
        [FromRoute] Guid userId,
        CancellationToken ct)
    {
        try
        {
            if (userId == Guid.Empty)
                return Results.BadRequest(new { error = "userId 不能为空" });

            var scope = await checker.GetUserDataScopeAsync(userId, ct);
            return Results.Ok(new { scopeType = (int)scope.Type, values = scope.Values.ToArray() });
        }
        catch (Exception)
        {
            return Results.Problem("获取数据范围失败", statusCode: StatusCodes.Status500InternalServerError);
        }
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

/// <summary>
/// 网关权限检查请求 DTO — 与 NotBlog_Yarp 侧 HttpPermissionServiceClient 的请求体一致
/// </summary>
public sealed record PermissionCheckRequest
{
    public Guid UserId { get; init; }

    public string PermissionCode { get; init; } = string.Empty;
}

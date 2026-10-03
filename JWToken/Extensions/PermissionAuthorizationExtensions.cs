using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Notcomd.Token.JWT.Core;

namespace Notcomd.Token.JWT.Extensions;

/// <summary>
/// 端点权限标注元数据：声明访问该端点所需的最小权限码（支持目录码，子孙自动放行）。
/// 由 <see cref="PermissionEnforcementMiddleware"/> 从 endpoint metadata 读取并判定。
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class RequirePermissionAttribute(string permissionCode) : Attribute
{
    /// <summary>所需权限码（如 api:tweet:read；授权目录码 api:tweet 也放行）</summary>
    public string PermissionCode { get; } = permissionCode;
}

/// <summary>
/// 服务内权限标注与强制扩展（授权收回服务后使用）：
/// - <see cref="RequirePermission"/>：端点级显式标注（可多个 = 满足其一即可）；
/// - <see cref="RequireResourcePermissions"/>：组级约定——按 HTTP 方法自动派生 read/create/update/delete，
///   与网关 URL→码映射 106 条语义等价（GET→read POST→create PUT→update DELETE→delete）；
/// - <see cref="UsePermissionEnforcement"/>：注册判定中间件（置于 UseAuthorization 之后）。
/// 判定读取 JWT permissions claim（逗号分隔授权码集合，含目录码），前缀段匹配与 Identity PermissionChecker 同逻辑。
/// </summary>
public static class PermissionAuthorizationExtensions
{
    /// <summary>
    /// 端点级显式标注：该端点要求指定权限码（标注多个 = 命中其一放行）。
    /// </summary>
    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, string permissionCode)
        where TBuilder : IEndpointConventionBuilder
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionCode);
        return builder.WithMetadata(new RequirePermissionAttribute(permissionCode));
    }

    /// <summary>
    /// 组级约定：组内每个端点按 HTTP 方法自动附加 {resourcePrefix}:{verb} 权限码。
    /// 已显式标注 <see cref="RequirePermissionAttribute"/> 的端点跳过（显式优先）。
    /// 约定沿组链传播：挂父组可覆盖全部嵌套子组端点（如 /api/filestorage 下 /chunk、/volume 等）。
    /// </summary>
    public static RouteGroupBuilder RequireResourcePermissions(
        this RouteGroupBuilder group, string resourcePrefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourcePrefix);

        ((IEndpointConventionBuilder)group).Add(endpointBuilder =>
        {
            // 显式标注优先：已存在 RequirePermissionAttribute 的端点不再自动派生
            if (endpointBuilder.Metadata.Any(m => m is RequirePermissionAttribute))
                return;

            var method = endpointBuilder.Metadata
                .OfType<HttpMethodMetadata>()
                .SelectMany(m => m.HttpMethods)
                .FirstOrDefault();

            var verb = method?.ToUpperInvariant() switch
            {
                "GET" or "HEAD" => "read",
                "POST" => "create",
                "PUT" or "PATCH" => "update",
                "DELETE" => "delete",
                _ => null
            };

            if (verb is not null)
                endpointBuilder.Metadata.Add(
                    new RequirePermissionAttribute($"{resourcePrefix}:{verb}"));
        });

        return group;
    }

    /// <summary>
    /// 注册服务内权限强制中间件（放在 UseAuthentication/UseAuthorization 之后）。
    /// </summary>
    public static IApplicationBuilder UsePermissionEnforcement(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<PermissionEnforcementMiddleware>();
    }
}

/// <summary>
/// 服务内权限强制中间件：
/// 1. endpoint 无权限标注 → 放行（保持现状：未标注端点由既有 [Authorize]/组级 RequireAuthorization 管控）；
/// 2. 有标注 → 未认证 401（Challenge）；已认证但无 permissions claim（旧 token）→ 401 引导重登；
/// 3. 授权码集合前缀段匹配任一标注码 → 放行；否则 403 JSON。
/// 判定纯本地（读 JWT claim），无网络回调；数据范围语义由各服务按需读 data_scope claim。
/// </summary>
public class PermissionEnforcementMiddleware(
    RequestDelegate next,
    ILogger<PermissionEnforcementMiddleware> logger)
{
    private const string ForbiddenPayload =
        "{\"error\":\"Forbidden\",\"message\":\"Insufficient permissions\"}";

    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint is null)
        {
            await next(context);
            return;
        }

        // 端点显式声明匿名访问（[AllowAnonymous] / .AllowAnonymous()）→ 放行。
        // AllowAnonymous 的语义就是「跳过授权」，本中间件不应例外；否则「公开端点」
        // 会被它当作未认证而 401（RequireAuthorization 已被 AllowAnonymous 抑制，这里也必须跟随）。
        if (endpoint.Metadata.OfType<IAllowAnonymous>().Any())
        {
            await next(context);
            return;
        }

        // 该端点要求的权限码集合（多个标注 = 命中任一即可）
        var required = endpoint.Metadata
            .OfType<RequirePermissionAttribute>()
            .Select(a => a.PermissionCode)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (required.Length == 0)
        {
            await next(context);
            return;
        }

        // 标注了权限的端点隐含"必须认证"
        if (context.User.Identity?.IsAuthenticated != true)
        {
            logger.LogWarning(
                "[PermissionEnforcement] 未认证访问受权限保护端点 Path={Path}", context.Request.Path);
            if (!context.Response.HasStarted)
                await context.ChallengeAsync();
            return;
        }

        // 无 permissions claim = 旧 token（升级前签发）→ 401 引导重新登录（403 会令前端误判且无法恢复）
        var claimValue = context.User.FindFirst(PermissionClaimTypes.Permissions)?.Value;
        if (claimValue is null)
        {
            logger.LogWarning(
                "[PermissionEnforcement] Token 无权限 claim（旧签发），要求重新登录 Path={Path}",
                context.Request.Path);
            if (!context.Response.HasStarted)
                await context.ChallengeAsync();
            return;
        }

        var granted = PermissionCodeMatcher.ParsePermissionClaim(claimValue);
        var hit = required.Any(code => PermissionCodeMatcher.HasPermission(granted, code));
        if (!hit)
        {
            logger.LogWarning(
                "[PermissionEnforcement] 权限拒绝 Path={Path} Required={Required}",
                context.Request.Path, string.Join(",", required));
            await WriteForbiddenAsync(context);
            return;
        }

        await next(context);
    }

    private static async Task WriteForbiddenAsync(HttpContext context)
    {
        if (context.Response.HasStarted)
            return;

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(ForbiddenPayload, context.RequestAborted);
    }
}

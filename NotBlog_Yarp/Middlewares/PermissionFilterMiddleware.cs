using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Notcomd.Token.JWT.Core;

namespace NotBlog_Yarp.Middlewares;

/// <summary>
/// 网关权限过滤中间件
///
/// 在 JWT 验证通过后、YARP 转发前执行，检查用户是否拥有访问目标路由的权限。
///
/// 验证方式（2026-09 起 JWT claim 本地判定优先）：
///   1. 匹配公开路径 → 直接放行
///   2. URL→PermissionCode 映射 → 未配置则放行（白名单模式）
///   3. 未认证 → 返回 401（带 WWW-Authenticate）
///   4a. Token 携带权限 claim（Identity 新签发：permissions=逗号分隔授权码集合，含目录码；
///       data_scope="type|v1,v2"）→ 本地前缀段匹配判定，不再回调 Identity；
///      授权目录码自动放行其全部子孙（与 Identity PermissionChecker 同逻辑）。
///      权限被拒 → 403 JSON；通过 → DataScope 存入 Items，放行。
///   4b. Token 无权限 claim（旧 token / 开发配置模式）→ 回退调用权限服务客户端远程校验。
///   5. 无权限 → 403 JSON；有权限 → DataScope 存入 Items，放行
///
/// 即时性说明：权限 claim 是签发时快照。角色授权变更时 Identity 会吊销受影响用户的全部
/// 会话（黑名单 + Redis），用户重登后携带新 claim——因此本地判定不会出现长时间撤权窗口。
/// </summary>
public class PermissionFilterMiddleware(
    RequestDelegate next,
    PermissionRouteMap routeMap,
    ILogger<PermissionFilterMiddleware> logger,
    IOptions<PermissionOptions> permissionOptions)
{
    private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));
    private readonly PermissionRouteMap _routeMap = routeMap ?? throw new ArgumentNullException(nameof(routeMap));
    private readonly ILogger<PermissionFilterMiddleware> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IOptions<PermissionOptions> _permissionOptions = permissionOptions ?? throw new ArgumentNullException(nameof(permissionOptions));

    /// <summary>HttpContext.Items 中存储 DataScope 的键</summary>
    public const string DataScopeItemKey = "NotBlog.DataScope";

    public async Task InvokeAsync(
        HttpContext context, IPermissionServiceClient permissionClient)
    {
        var path = context.Request.Path.Value ?? "/";
        var method = context.Request.Method;

        if (_routeMap.IsPublicPath(path))
        {
            _logger.LogDebug("[PermissionFilter] 公开路径放行 Path={Path}", path);
            await _next(context);
            return;
        }

        if (_routeMap.TryMatch(path, method, out var permissionCode))
        {
            if (context.User.Identity?.IsAuthenticated != true)
            {
                _logger.LogWarning("[PermissionFilter] 未认证拒绝 Path={Path}", path);

                if (!context.Response.HasStarted)
                {
                    await context.ChallengeAsync();
                }
                return;
            }

            var userId = GetUserId(context.User);
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                _logger.LogWarning(
                    "[PermissionFilter] 无法提取有效 UserId Path={Path} Raw={RawUserId}", path, userId);
                await WriteForbiddenAsync(context);
                return;
            }

            // ── 方式 A（默认）：JWT claim 本地判定 ──
            // Identity 新签发的 token 携带 permissions（逗号分隔授权码集）与 data_scope claim。
            if (TryLocalCheck(context, permissionCode, out var localGranted))
            {
                if (!localGranted)
                {
                    _logger.LogWarning(
                        "[PermissionFilter] 本地判定拒绝 UserId={UserId} Code={Code} Path={Path} Method={Method}",
                        userId, permissionCode, path, method);
                    await WriteForbiddenAsync(context);
                    return;
                }

                context.Items[DataScopeItemKey] = ResolveLocalDataScope(context);
                _logger.LogInformation(
                    "[PermissionFilter] 本地判定通过 UserId={UserId} Code={Code} Path={Path}",
                    userId, permissionCode, path);
                await _next(context);
                return;
            }

            // ── 方式 B（回退）：token 无权限 claim（旧 token / DevUsers 配置模式）→ 远程校验 ──
            PermissionCheckResult result;
            try
            {
                result = await permissionClient.CheckAndGetScopeAsync(
                    userGuid, permissionCode, context.RequestAborted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[PermissionFilter] 权限服务调用异常 UserId={UserId} Code={Code}（FailPolicy={Policy}）",
                    userId, permissionCode, _permissionOptions.Value.FailPolicy);

                if (_permissionOptions.Value.FailOpen)
                {
                    _logger.LogWarning(
                        "[PermissionFilter] 权限服务异常，按 FailPolicy=Open 放行 Path={Path}",
                        path);
                    await _next(context);
                    return;
                }

                await WriteForbiddenAsync(context);
                return;
            }

            if (!result.HasPermission)
            {
                _logger.LogWarning(
                    "[PermissionFilter] 权限拒绝 UserId={UserId} Code={Code} Path={Path} Method={Method}",
                    userId, permissionCode, path, method);
                await WriteForbiddenAsync(context);
                return;
            }

            // 存入 DataScope 供 UserContextTransform 注入 Header
            context.Items[DataScopeItemKey] = result.DataScope;

            _logger.LogInformation(
                "[PermissionFilter] 通过 UserId={UserId} Code={Code} Path={Path}",
                userId, permissionCode, path);
        }
        else
        {
            // 未配置权限映射的路径 → 默认拒绝（P0-V3，白名单模式）：
            //    未映射 = 不受保护，默认 403；仅当 DefaultPolicy=Allow（开发环境）时放行
            if (!_permissionOptions.Value.DefaultAllow)
            {
                _logger.LogWarning(
                    "[PermissionFilter] 未映射路径默认拒绝 Path={Path} Method={Method}", path, method);
                await WriteForbiddenAsync(context);
                return;
            }

            _logger.LogDebug(
                "[PermissionFilter] 未映射路径放行（DefaultPolicy=Allow）Path={Path} Method={Method}", path, method);
        }

        await _next(context);
    }

    /// <summary>
    /// 从 JWT claims 本地判定权限。返回 false 表示 token 无权限 claim（应回退远程校验）。
    /// 判定规则与 Identity PermissionChecker 一致：目标码命中 = 授权集合含其自身，
    /// 或含其某个祖先段码（授权目录 api:tweet 自动放行 api:tweet:read 等全部子孙）。
    /// </summary>
    private static bool TryLocalCheck(HttpContext context, string permissionCode, out bool granted)
    {
        granted = false;

        var rawClaims = context.User.FindAll(PermissionClaimTypes.Permissions).ToList();
        if (rawClaims.Count == 0)
            return false; // 无 claim → 走远程回退

        var grantedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var claim in rawClaims)
        {
            if (string.IsNullOrWhiteSpace(claim.Value))
                continue;
            foreach (var code in claim.Value.Split(',',
                         StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                grantedCodes.Add(code);
        }

        // 前缀段匹配：目标码自身 ∈ 授权集，或任一授权码 a 满足 code.StartsWith(a + ":")
        granted = grantedCodes.Contains(permissionCode)
                  || grantedCodes.Any(a => permissionCode.StartsWith(a + ":", StringComparison.OrdinalIgnoreCase));
        return true;
    }

    /// <summary>解析 token 内 data_scope claim（"type|v1,v2"）；缺失/无效回退 Own（"0|"）</summary>
    private static Dictionary<string, HashSet<string>> ResolveLocalDataScope(HttpContext context)
    {
        var fallback = new Dictionary<string, HashSet<string>> { { "0", new HashSet<string>() } };
        var value = context.User.FindFirst(PermissionClaimTypes.DataScope)?.Value;
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        var parts = value.Split('|', 2);
        if (parts.Length < 1 || string.IsNullOrEmpty(parts[0]))
            return fallback;

        var values = parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1])
            ? parts[1].Split(',', StringSplitOptions.RemoveEmptyEntries)
            : Array.Empty<string>();
        return new Dictionary<string, HashSet<string>>
        {
            { parts[0], new HashSet<string>(values, StringComparer.OrdinalIgnoreCase) }
        };
    }

    /// <summary>
    /// 从 ClaimsPrincipal 提取用户 ID
    /// </summary>
    private static string? GetUserId(ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")?.Value
            ?? user.FindFirst("id")?.Value;
    }

    /// <summary>
    /// 写入统一的 403 Forbidden JSON 响应
    /// </summary>
    private static async Task WriteForbiddenAsync(HttpContext context)
    {
        if (context.Response.HasStarted)
            return;

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(
            "{\"error\":\"Forbidden\",\"message\":\"Insufficient permissions\"}",
            context.RequestAborted);
    }
}

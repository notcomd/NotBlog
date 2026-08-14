using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace NotBlog_Yarp.Permission;

/// <summary>
/// 基于 HTTP 的权限服务客户端
/// 
/// 通过 HTTP 调用 Identity.Web.API 暴露的权限检查 REST 端点。
/// 
/// 预期 Identity 侧端点（⚠️ 双 permission 段：MapGroup api/identity/permission + 子组 permission）：
///   POST /api/identity/permission/permission/check → { userId, permissionCode } → { hasPermission: bool }
///   GET  /api/identity/permission/permission/datascope/{userId} → { scopeType, values }
///
/// 失败降级（F-12）：Identity 不可用/端点缺失时按 PermissionOptions.FailPolicy 处理
/// （Open = 放行 / Closed = 拒绝），避免权限服务短暂不可用时全站 403。
/// </summary>
public class HttpPermissionServiceClient : IPermissionServiceClient
{
    private readonly HttpClient _http;
    private readonly ILogger<HttpPermissionServiceClient> _logger;
    private readonly IOptions<PermissionOptions> _options;

    public HttpPermissionServiceClient(
        HttpClient http,
        ILogger<HttpPermissionServiceClient> logger,
        IOptions<PermissionOptions> options)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PermissionMappingDto>> GetMappingsAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync("/api/identity/permission/permission/mappings", ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "[HttpPermissionClient] GetMappings 非成功 {StatusCode}", (int)response.StatusCode);
                return Array.Empty<PermissionMappingDto>();
            }

            var mappings = await response.Content
                .ReadFromJsonAsync<List<PermissionMappingDto>>(ct);

            _logger.LogInformation(
                "[HttpPermissionClient] 从 Identity 获取了 {Count} 条路由映射", mappings?.Count ?? 0);

            return mappings?.AsReadOnly()
                   ?? (IReadOnlyList<PermissionMappingDto>)Array.Empty<PermissionMappingDto>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "[HttpPermissionClient] 无法连接 Identity 获取路由映射");
            return Array.Empty<PermissionMappingDto>();
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("[HttpPermissionClient] GetMappings 请求超时");
            return Array.Empty<PermissionMappingDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[HttpPermissionClient] GetMappings 异常");
            return Array.Empty<PermissionMappingDto>();
        }
    }

    /// <inheritdoc />
    public async Task<PermissionCheckResult> CheckAndGetScopeAsync(
        Guid userId, string permissionCode, CancellationToken ct = default)
    {
        if (userId == Guid.Empty || string.IsNullOrWhiteSpace(permissionCode))
            return PermissionCheckResult.Denied();

        try
        {
            var payload = new { userId = userId.ToString(), permissionCode };
            var response = await _http.PostAsJsonAsync(
                "/api/identity/permission/permission/check-and-scope", payload, ct);

            if (!response.IsSuccessStatusCode)
            {
                // V5：仅 5xx/429 视为"服务不可用"按 FailPolicy 降级；
                // 4xx（404 端点缺失 / 401 凭证错误 / 400 参数错误）属于配置或调用错误，
                // fail-closed 拒绝 —— 否则权限检查会因端点缺失而静默放行。
                if ((int)response.StatusCode >= 500 || (int)response.StatusCode == StatusCodes.Status429TooManyRequests)
                {
                    _logger.LogWarning(
                        "[HttpPermissionClient] CheckAndGetScope 服务不可用 {StatusCode} UserId={UserId} Code={Code}（FailPolicy={Policy}）",
                        (int)response.StatusCode, userId, permissionCode, _options.Value.FailPolicy);
                    return FailOpenResult();
                }

                _logger.LogError(
                    "[HttpPermissionClient] CheckAndGetScope 返回 {StatusCode}（4xx，fail-closed 拒绝）UserId={UserId} Code={Code}",
                    (int)response.StatusCode, userId, permissionCode);
                return PermissionCheckResult.Denied();
            }

            var result = await response.Content.ReadFromJsonAsync<CombinedResult>(ct);

            _logger.LogDebug(
                "[HttpPermissionClient] CheckAndGetScope UserId={UserId} Code={Code} HasPerm={HasPerm} Scope={Scope}",
                userId, permissionCode, result?.HasPermission ?? false, result?.DataScope);

            if (result?.HasPermission != true)
                return PermissionCheckResult.Denied();

            return PermissionCheckResult.Granted(result.DataScope ?? "0|");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "[HttpPermissionClient] CheckAndGetScope 无法连接 Identity UserId={UserId}", userId);
            return FailOpenResult();
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning(
                "[HttpPermissionClient] CheckAndGetScope 超时 UserId={UserId}", userId);
            return FailOpenResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[HttpPermissionClient] CheckAndGetScope 异常 UserId={UserId} Code={Code}", userId, permissionCode);
            return FailOpenResult();
        }
    }

    /// <inheritdoc />
    public async Task<bool> CheckPermissionAsync(
        Guid userId, string permissionCode, CancellationToken ct = default)
    {
        if (userId == Guid.Empty || string.IsNullOrWhiteSpace(permissionCode))
            return false;

        try
        {
            var payload = new { userId = userId.ToString(), permissionCode };
            var response = await _http.PostAsJsonAsync("/api/identity/permission/permission/check", payload, ct);

            if (!response.IsSuccessStatusCode)
            {
                // V5：同 CheckAndGetScopeAsync —— 仅 5xx/429 按策略降级，4xx fail-closed
                if ((int)response.StatusCode >= 500 || (int)response.StatusCode == StatusCodes.Status429TooManyRequests)
                {
                    _logger.LogWarning(
                        "[HttpPermissionClient] CheckPermission 服务不可用 {StatusCode} UserId={UserId} Code={Code}（FailPolicy={Policy}）",
                        (int)response.StatusCode, userId, permissionCode, _options.Value.FailPolicy);
                    return FailOpen();
                }

                _logger.LogError(
                    "[HttpPermissionClient] CheckPermission 返回 {StatusCode}（4xx，fail-closed 拒绝）UserId={UserId} Code={Code}",
                    (int)response.StatusCode, userId, permissionCode);
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<CheckResult>(ct);
            _logger.LogDebug(
                "[HttpPermissionClient] CheckPermission UserId={UserId} Code={Code} Result={Result}",
                userId, permissionCode, result?.HasPermission ?? false);

            return result?.HasPermission ?? false;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "[HttpPermissionClient] 无法连接 Identity 服务 UserId={UserId} Code={Code}",
                userId, permissionCode);
            return FailOpen(); // 降级策略：Open 放行 / Closed 拒绝
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning(
                "[HttpPermissionClient] 请求超时 UserId={UserId} Code={Code}", userId, permissionCode);
            return FailOpen();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[HttpPermissionClient] 权限检查异常 UserId={UserId} Code={Code}", userId, permissionCode);
            return FailOpen();
        }
    }

    /// <inheritdoc />
    public async Task<string> GetDataScopeAsync(Guid userId, CancellationToken ct = default)
    {
        if (userId == Guid.Empty)
            return "0|"; // Own, no values

        try
        {
            var response = await _http.GetAsync(
                $"/api/identity/permission/permission/datascope/{userId}", ct);

            if (!response.IsSuccessStatusCode)
            {
                // V5：任何非成功状态码都按默认 Own 范围（"0|"）处理（最严格），并记录错误日志
                _logger.LogError(
                    "[HttpPermissionClient] GetDataScope 返回 {StatusCode}，按默认 Own 范围处理 UserId={UserId}",
                    (int)response.StatusCode, userId);
                return "0|";
            }

            var result = await response.Content.ReadFromJsonAsync<DataScopeResult>(ct);
            var scopeValue = result?.ToClaimValue() ?? "0|";

            _logger.LogDebug(
                "[HttpPermissionClient] GetDataScope UserId={UserId} Scope={Scope}", userId, scopeValue);

            return scopeValue;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "[HttpPermissionClient] 无法连接 Identity 获取 DataScope UserId={UserId}", userId);
            return "0|";
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning(
                "[HttpPermissionClient] GetDataScope 超时 UserId={UserId}", userId);
            return "0|";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[HttpPermissionClient] GetDataScope 异常 UserId={UserId}", userId);
            return "0|";
        }
    }

    /// <summary>
    /// 权限服务失败时的降级结果（F-12 / V5）：
    /// 仅由 5xx/429（服务不可用）触发；FailPolicy=Open → 放行（返回通过 + 默认 Own 数据范围），
    /// Closed → 拒绝。4xx 不走此路径（fail-closed）。
    /// </summary>
    private PermissionCheckResult FailOpenResult()
    {
        if (_options.Value.FailOpen)
        {
            _logger.LogWarning("[HttpPermissionClient] 权限服务不可用，按 FailPolicy=Open 放行（fail-open 降级）");
            return PermissionCheckResult.Granted("0|");
        }
        return PermissionCheckResult.Denied();
    }

    /// <summary>权限服务失败时的布尔降级结果（F-12 / V5）：仅由 5xx/429 触发；Open → true（放行），Closed → false（拒绝）</summary>
    private bool FailOpen()
    {
        if (_options.Value.FailOpen)
        {
            _logger.LogWarning("[HttpPermissionClient] 权限服务不可用，按 FailPolicy=Open 放行（fail-open 降级）");
            return true;
        }
        return false;
    }

    private sealed class CombinedResult
    {
        public bool HasPermission { get; init; }
        public string? DataScope { get; init; }
    }

    private sealed class CheckResult
    {
        public bool HasPermission { get; init; }
    }

    private sealed class DataScopeResult
    {
        public int ScopeType { get; init; }
        public string[]? Values { get; init; }

        public string ToClaimValue()
        {
            var valuesStr = Values is { Length: > 0 }
                ? string.Join(",", Values)
                : string.Empty;
            return $"{ScopeType}|{valuesStr}";
        }
    }
}

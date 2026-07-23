using System.Net.Http.Json;

namespace NotBlog_Yarp.Permission;

/// <summary>
/// 基于 HTTP 的权限服务客户端
/// 
/// 通过 HTTP 调用 Identity.Web.API 暴露的权限检查 REST 端点。
/// 
/// 预期 Identity 侧端点：
///   POST /api/permission/check  → { userId, permissionCode } → { hasPermission: bool }
///   GET  /api/permission/datascope/{userId} → { scopeType, values }
/// </summary>
public class HttpPermissionServiceClient : IPermissionServiceClient
{
    private readonly HttpClient _http;
    private readonly ILogger<HttpPermissionServiceClient> _logger;

    public HttpPermissionServiceClient(HttpClient http, ILogger<HttpPermissionServiceClient> logger)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PermissionMappingDto>> GetMappingsAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync("api/ready/permission/mappings", ct);

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
                "/api/permission/check-and-scope", payload, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "[HttpPermissionClient] CheckAndGetScope 非成功 {StatusCode} UserId={UserId} Code={Code}",
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
            return PermissionCheckResult.Denied();
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning(
                "[HttpPermissionClient] CheckAndGetScope 超时 UserId={UserId}", userId);
            return PermissionCheckResult.Denied();
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
            var response = await _http.PostAsJsonAsync("/api/permission/check", payload, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "[HttpPermissionClient] Identity 返回非成功状态码 {StatusCode} UserId={UserId} Code={Code}",
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
            return false; // fail-closed: 无法确认权限时拒绝
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning(
                "[HttpPermissionClient] 请求超时 UserId={UserId} Code={Code}", userId, permissionCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[HttpPermissionClient] 权限检查异常 UserId={UserId} Code={Code}", userId, permissionCode);
            return false;
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
                $"/api/permission/datascope/{userId}", ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "[HttpPermissionClient] GetDataScope 非成功状态码 {StatusCode} UserId={UserId}",
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

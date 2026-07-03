using CacheMemory.Core;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CacheMemory.Providers;

/// <summary>
/// CacheMemory Redis 健康检查实现。
/// 通过尝试 Ping Redis 服务器来验证连接状态，支持 Aspire 的 /health 端点。
/// </summary>
public sealed class CacheMemoryHealthCheck(
    IRedisConnectionProvider connectionProvider,
    ILogger<CacheMemoryHealthCheck>? logger = null)
    : IHealthCheck
{
    /// <summary>
    /// 健康检查名称，用于 DI 注册和健康检查端点过滤。
    /// </summary>
    public const string Name = "Redis-CacheMemory";

    private readonly IRedisConnectionProvider _connectionProvider =
        connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));

    private readonly ILogger<CacheMemoryHealthCheck> _logger = logger ?? NullLogger<CacheMemoryHealthCheck>.Instance;

    /// <summary>
    /// 检查所有已注册的 Redis 实例连接是否健康。
    /// </summary>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var instanceNames = _connectionProvider.GetInstanceNames();
        var unhealthyInstances = new List<string>();

        foreach (var instanceName in instanceNames)
        {
            try
            {
                var conn = _connectionProvider.GetConnection(instanceName);
                if (conn.IsConnected)
                {
                    var db = conn.GetDatabase();
                    var latency = await db.PingAsync().ConfigureAwait(false);
                    _logger.LogDebug(
                        "Redis 健康检查通过 [{InstanceName}]，延迟 {LatencyMs}ms",
                        instanceName, latency.TotalMilliseconds);
                }
                else
                {
                    _logger.LogWarning("Redis 实例 [{InstanceName}] 未连接", instanceName);
                    unhealthyInstances.Add(instanceName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis 健康检查失败 [{InstanceName}]", instanceName);
                unhealthyInstances.Add(instanceName);
            }
        }

        if (unhealthyInstances.Count == 0)
        {
            return HealthCheckResult.Healthy(
                $"所有 {instanceNames.Count} 个 Redis 实例连接正常",
                new Dictionary<string, object>
                {
                    ["InstanceCount"] = instanceNames.Count
                });
        }

        if (unhealthyInstances.Count == instanceNames.Count)
        {
            return HealthCheckResult.Unhealthy(
                $"所有 Redis 实例连接失败: {string.Join(", ", unhealthyInstances)}");
        }

        return HealthCheckResult.Degraded(
            $"部分 Redis 实例连接失败: {string.Join(", ", unhealthyInstances)}。" +
            $"正常: {instanceNames.Count - unhealthyInstances.Count}/{instanceNames.Count}");
    }
}
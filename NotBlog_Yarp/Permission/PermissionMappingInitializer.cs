using Microsoft.Extensions.Options;

namespace NotBlog_Yarp.Permission;

/// <summary>
/// 权限路由映射加载器 — 启动时从 Identity 服务拉取 URL→PermissionCode 映射
/// 
/// 加载策略:
///   1. 先使用 appsettings.json 中的 Mappings 作为初始值（兜底）
///   2. 启动后尝试从 Identity 拉取最新映射
///   3. 成功 → 替换为 Identity 的映射（单一权威来源）
///   4. 失败 → 保留本地配置映射（有警告日志）
/// 
/// 作为 IHostedService，应用启动后自动执行，不阻塞启动流程。
/// </summary>
public class PermissionMappingInitializer : BackgroundService
{
    private readonly PermissionRouteMap _routeMap;
    private readonly IPermissionServiceClient _client;
    private readonly IOptions<PermissionOptions> _options;
    private readonly ILogger<PermissionMappingInitializer> _logger;

    public PermissionMappingInitializer(
        PermissionRouteMap routeMap,
        IPermissionServiceClient client,
        IOptions<PermissionOptions> options,
        ILogger<PermissionMappingInitializer> logger)
    {
        _routeMap = routeMap ?? throw new ArgumentNullException(nameof(routeMap));
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "[MappingLoader] 开始从 Identity 加载路由映射 RouteMap 本地条目={LocalCount}",
            _routeMap.Count);

        try
        {
            // 稍等 Identity 完成启动（健康检查 + 路由注册）
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

            var maxRetries = 3;
            for (var retry = 0; retry < maxRetries; retry++)
            {
                stoppingToken.ThrowIfCancellationRequested();

                try
                {
                    var mappings = await _client.GetMappingsAsync(stoppingToken);

                    if (mappings.Count > 0)
                    {
                        // 替换保护（P0-V2）：远程映射显著少于本地映射时，视为 Identity 侧
                        // PermissionMappings 未同步完整（配置漂移），拒绝替换并保留本地配置，
                        // 防止大量路由退回"未映射"状态而失去权限保护。
                        var localCount = _routeMap.Count;
                        if (mappings.Count < Math.Max(1, localCount / 2))
                        {
                            _logger.LogError(
                                "[MappingLoader] 拒绝替换：Identity 返回 {RemoteCount} 条映射，远少于本地 {LocalCount} 条（< 50%），视为配置异常，保留本地映射",
                                mappings.Count, localCount);
                            return;
                        }

                        // 替换本地配置的映射为 Identity 的权威映射
                        _routeMap.ClearMappings();
                        foreach (var m in mappings)
                            _routeMap.AddMap(m.Method, m.Path, m.Code);

                        _logger.LogInformation(
                            "[MappingLoader] 已从 Identity 加载 {Count} 条路由映射，本地映射已替换",
                            mappings.Count);
                        return; // 成功
                    }

                    _logger.LogWarning(
                        "[MappingLoader] Identity 返回空映射列表（第 {Retry}/{Max} 次）",
                        retry + 1, maxRetries);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex,
                        "[MappingLoader] 加载失败（第 {Retry}/{Max} 次），将保持本地配置映射",
                        retry + 1, maxRetries);
                }

                if (retry < maxRetries - 1)
                    await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }

            _logger.LogWarning(
                "[MappingLoader] 多次重试后仍无法从 Identity 加载映射，使用本地配置作为兜底（{Count} 条）",
                _routeMap.Count);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("[MappingLoader] 启动加载被取消，使用本地配置映射");
        }
    }
}

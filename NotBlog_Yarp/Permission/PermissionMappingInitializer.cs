using Microsoft.Extensions.Options;

namespace NotBlog_Yarp.Permission;

/// <summary>
/// 权限路由映射加载器 — 启动时加载 URL→PermissionCode 映射
///
/// 加载策略（三级降级）:
///   1. 先从本地 JSON 文件加载（快速回退，Identity 不可用时仍可用）
///   2. 再从 Identity 拉取权威映射 → 替换 + 持久化到 JSON
///   3. Identity 失败 → 保留 JSON 或 appsettings 中的本地配置映射
///
/// 作为 IHostedService，应用启动后自动执行，不阻塞启动流程。
/// </summary>
public class PermissionMappingInitializer : BackgroundService
{
    private readonly PermissionRouteMap _routeMap;
    private readonly IPermissionServiceClient _client;
    private readonly PermissionMappingStore _store;
    private readonly IOptions<PermissionOptions> _options;
    private readonly ILogger<PermissionMappingInitializer> _logger;

    public PermissionMappingInitializer(
        PermissionRouteMap routeMap,
        IPermissionServiceClient client,
        PermissionMappingStore store,
        IOptions<PermissionOptions> options,
        ILogger<PermissionMappingInitializer> logger)
    {
        _routeMap = routeMap ?? throw new ArgumentNullException(nameof(routeMap));
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "[MappingLoader] 开始加载路由映射 RouteMap 本地条目={LocalCount}",
            _routeMap.Count);

        try
        {
            // 步骤 1：尝试从本地 JSON 加载（秒级，不依赖 Identity）
            var cached = await _store.LoadAsync(stoppingToken);
            if (cached is { Count: > 0 } cachedMappings)
            {
                _routeMap.ReplaceAll(
                    cachedMappings.Select(m => (m.Method, m.Path, m.Code)));
                _logger.LogInformation(
                    "[MappingLoader] 已从本地 JSON 加载 {Count} 条映射作为初始值",
                    cachedMappings.Count);
            }

            // 步骤 2：稍等 Identity 完成启动，然后拉取权威映射
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
                        // 替换保护：远程映射显著少于本地时拒绝
                        var localCount = _routeMap.Count;
                        if (mappings.Count < Math.Max(1, localCount / 2))
                        {
                            _logger.LogError(
                                "[MappingLoader] 拒绝替换：Identity 返回 {RemoteCount} 条映射，远少于本地 {LocalCount} 条（< 50%），视为配置异常，保留本地映射",
                                mappings.Count, localCount);
                            return;
                        }

                        // 原子替换 + 持久化到 JSON
                        _routeMap.ReplaceAll(
                            mappings.Select(m => (m.Method, m.Path, m.Code)));
                        await _store.SaveAsync(mappings, stoppingToken);

                        _logger.LogInformation(
                            "[MappingLoader] 已从 Identity 加载 {Count} 条路由映射，本地映射已替换并持久化",
                            mappings.Count);
                        return;
                    }

                    _logger.LogWarning(
                        "[MappingLoader] Identity 返回空映射列表（第 {Retry}/{Max} 次）",
                        retry + 1, maxRetries);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex,
                        "[MappingLoader] 加载失败（第 {Retry}/{Max} 次），将保持本地映射",
                        retry + 1, maxRetries);
                }

                if (retry < maxRetries - 1)
                    await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }

            _logger.LogWarning(
                "[MappingLoader] 多次重试后仍无法从 Identity 加载映射，使用本地映射作为兜底（{Count} 条）",
                _routeMap.Count);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("[MappingLoader] 启动加载被取消，使用本地映射");
        }
    }
}

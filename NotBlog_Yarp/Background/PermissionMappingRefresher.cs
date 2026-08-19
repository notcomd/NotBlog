using Microsoft.Extensions.Options;

namespace NotBlog_Yarp.Background;

/// <summary>
/// 权限映射后台轮询服务 — 定期从 Identity 静默拉取最新路由映射
///
/// 设计要点：
///   - 仅在生产模式（IdentityService:BaseUrl 已配置）下注册
///   - 默认 5 分钟轮询一次（RefreshIntervalSeconds 可配，设为 0 禁用）
///   - 复用 PermissionMappingInitializer 的替换保护逻辑（远程 < 本地 50% 拒绝替换）
///   - 使用 PermissionRouteMap.ReplaceAll 原子替换，避免 Clear→Add 空窗口
///   - 单次拉取失败不中断后续轮询，仅记录 Warning 日志
/// </summary>
public class PermissionMappingRefresher : BackgroundService
{
    private readonly PermissionRouteMap _routeMap;
    private readonly IPermissionServiceClient _client;
    private readonly PermissionMappingStore _store;
    private readonly IOptionsMonitor<PermissionOptions> _options;
    private readonly ILogger<PermissionMappingRefresher> _logger;

    public PermissionMappingRefresher(
        PermissionRouteMap routeMap,
        IPermissionServiceClient client,
        PermissionMappingStore store,
        IOptionsMonitor<PermissionOptions> options,
        ILogger<PermissionMappingRefresher> logger)
    {
        _routeMap = routeMap ?? throw new ArgumentNullException(nameof(routeMap));
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = _options.CurrentValue.RefreshIntervalSeconds;
        if (interval <= 0)
        {
            _logger.LogInformation(
                "[MappingRefresher] 后台刷新已禁用（RefreshIntervalSeconds={Seconds}）", interval);
            return;
        }

        _logger.LogInformation(
            "[MappingRefresher] 启动后台轮询，间隔 {Interval}s", interval);

        // 首次延迟：等 PermissionMappingInitializer 完成启动加载后再开始轮询
        var initialDelay = TimeSpan.FromSeconds(Math.Max(10, interval / 2));
        await Task.Delay(initialDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RefreshMappingsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "[MappingRefresher] 轮询异常，将在下个周期重试");
            }

            // 重新读取间隔（支持运行时热重载）
            interval = _options.CurrentValue.RefreshIntervalSeconds;
            if (interval <= 0)
            {
                _logger.LogInformation("[MappingRefresher] 运行中检测到刷新已禁用，停止轮询");
                break;
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(interval), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("[MappingRefresher] 后台轮询已停止");
    }

    /// <summary>
    /// 执行一次映射刷新：拉取 → 替换保护 → 原子替换
    /// </summary>
    private async Task RefreshMappingsAsync(CancellationToken ct)
    {
        var mappings = await _client.GetMappingsAsync(ct);

        if (mappings.Count == 0)
        {
            _logger.LogDebug("[MappingRefresher] Identity 返回空映射，跳过本次刷新");
            return;
        }

        var localCount = _routeMap.Count;

        // 替换保护：远程映射显著少于本地时视为配置异常，拒绝替换
        if (localCount > 0 && mappings.Count < Math.Max(1, localCount / 2))
        {
            _logger.LogWarning(
                "[MappingRefresher] 拒绝替换：Identity 返回 {RemoteCount} 条映射，远少于本地 {LocalCount} 条（< 50%），视为配置异常",
                mappings.Count, localCount);
            return;
        }

        // 原子替换 + 持久化
        _routeMap.ReplaceAll(mappings.Select(m => (m.Method, m.Path, m.Code)));
        await _store.SaveAsync(mappings, ct);

        _logger.LogInformation(
            "[MappingRefresher] 已刷新路由映射：{RemoteCount} 条（原 {LocalCount} 条），已持久化",
            mappings.Count, localCount);
    }
}

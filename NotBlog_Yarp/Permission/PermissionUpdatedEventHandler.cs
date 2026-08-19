using Notcomd.EventBus.Core;

namespace NotBlog_Yarp.Permission;

/// <summary>
/// 权限变更事件处理器 — 接收 Identity 发布的 PermissionUpdatedIntegrationEvent
///
/// 处理流程：
///   1. 收到事件后从 Identity 拉取最新路由映射
///   2. 通过 PermissionRouteMap.ReplaceAll 原子替换
///   3. 持久化到本地 JSON 文件（PermissionMappingStore）
///
/// 事件驱动 + 轮询双保险：
///   - 事件：Identity 权限 CRUD 后秒级感知
///   - 轮询（PermissionMappingRefresher）：兜底防漏
/// </summary>
public class PermissionUpdatedEventHandler
    : IIntegrationEventHandler<PermissionUpdatedIntegrationEvent>
{
    private readonly PermissionRouteMap _routeMap;
    private readonly IPermissionServiceClient _client;
    private readonly PermissionMappingStore _store;
    private readonly ILogger<PermissionUpdatedEventHandler> _logger;

    public PermissionUpdatedEventHandler(
        PermissionRouteMap routeMap,
        IPermissionServiceClient client,
        PermissionMappingStore store,
        ILogger<PermissionUpdatedEventHandler> logger)
    {
        _routeMap = routeMap ?? throw new ArgumentNullException(nameof(routeMap));
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Handler(PermissionUpdatedIntegrationEvent @event)
    {
        _logger.LogInformation(
            "[PermissionEventHandler] 收到权限变更事件: Action={Action}, PermissionId={PermissionId}",
            @event.Action, @event.PermissionId);

        try
        {
            var mappings = await _client.GetMappingsAsync();

            if (mappings.Count == 0)
            {
                _logger.LogWarning(
                    "[PermissionEventHandler] Identity 返回空映射，跳过更新（可能是配置异常）");
                return;
            }

            // 替换保护：远程映射显著少于本地时拒绝
            var localCount = _routeMap.Count;
            if (localCount > 0 && mappings.Count < Math.Max(1, localCount / 2))
            {
                _logger.LogWarning(
                    "[PermissionEventHandler] 拒绝替换：远程 {RemoteCount} < 本地 {LocalCount} 的 50%",
                    mappings.Count, localCount);
                return;
            }

            // 原子替换 + 持久化
            _routeMap.ReplaceAll(mappings.Select(m => (m.Method, m.Path, m.Code)));
            await _store.SaveAsync(mappings);

            _logger.LogInformation(
                "[PermissionEventHandler] 权限映射已更新并持久化: {RemoteCount} 条（原 {LocalCount} 条）",
                mappings.Count, localCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[PermissionEventHandler] 处理权限变更事件失败: Action={Action}, PermissionId={PermissionId}",
                @event.Action, @event.PermissionId);
        }
    }
}

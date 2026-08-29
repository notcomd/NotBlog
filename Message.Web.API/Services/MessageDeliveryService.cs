
namespace Message.Web.API.Services;

/// <summary>
/// 消息实时推送服务。
/// <para>
/// 统一封装 SignalR 向客户端推送消息的能力：
/// - 以 Redis 连接管理器为权威来源，跨实例按用户连接并行推送（高并发下响应快、无重复）；
/// - 支持 SignalR 会话群组（session:{id}）作为附加通道；
/// - 提供上传进度、已读回执、输入指示等实时事件的推送接口。
/// </para>
/// 该服务被 <see cref="MessageHub"/> 与文件分片上传接口共同使用。
/// </summary>
public class MessageDeliveryService
{
    private readonly IHubContext<MessageHub, IMessageClient> _hubContext;
    private readonly IConnectionManager _connectionManager;
    private readonly ILogger<MessageDeliveryService> _logger;

    public MessageDeliveryService(
        IHubContext<MessageHub, IMessageClient> hubContext,
        IConnectionManager connectionManager,
        ILogger<MessageDeliveryService> logger)
    {
        _hubContext = hubContext;
        _connectionManager = connectionManager;
        _logger = logger;
    }

    /// <summary>会话对应的 SignalR 群组名</summary>
    public static string SessionGroupName(Guid sessionId) => $"session:{sessionId}";

    /// <summary>
    /// 并行获取一批用户的全部在线连接（去重）。
    /// </summary>
    public async Task<IReadOnlyList<string>> GetOnlineConnectionsAsync(
        IEnumerable<Guid> userIds, CancellationToken ct = default)
    {
        var distinctIds = userIds.Distinct().ToArray();
        if (distinctIds.Length == 0)
            return Array.Empty<string>();

        ct.ThrowIfCancellationRequested();

        // 并行查询所有参与者的连接，避免顺序调用造成的响应延迟
        var results = await Task.WhenAll(
            distinctIds.Select(id => _connectionManager.GetConnectionsAsync(id)));

        return results.SelectMany(r => r).Distinct().ToArray();
    }

    /// <summary>
    /// 将消息推送给会话所有参与者（含发送者自身的其他设备）。
    /// 支持排除指定连接（例如发送者当前调用连接，由调用方决定是否回显）。
    /// </summary>
    public async Task DeliverMessageAsync(
        Guid sessionId, MessageDto message, IEnumerable<Guid> participantIds,
        string? excludeConnectionId = null, CancellationToken ct = default)
    {
        var connections = await GetOnlineConnectionsAsync(participantIds, ct);
        if (connections.Count == 0)
            return;

        var targets = excludeConnectionId is null
            ? connections
            : connections.Where(c => c != excludeConnectionId).ToArray();

        if (targets.Count == 0)
            return;

        _logger.LogDebug("会话 {SessionId} 推送消息 {MessageId} 到 {ConnectionCount} 个连接",
            sessionId, message.MessageId, targets.Count);

        // 并行向所有目标连接推送，降低整体延迟
        await Task.WhenAll(
            targets.Select(c => _hubContext.Clients.Client(c).ReceiveMessage(message)));
    }

    /// <summary>
    /// 通知会话内其他用户某成员正在输入。
    /// </summary>
    public async Task NotifyTypingAsync(
        Guid sessionId, Guid userId, IEnumerable<Guid> participantIds, CancellationToken ct = default)
    {
        var others = participantIds.Where(p => p != userId);
        var connections = await GetOnlineConnectionsAsync(others, ct);

        await Task.WhenAll(
            connections.Select(c => _hubContext.Clients.Client(c).TypingIndicator(sessionId, userId)));
    }

    /// <summary>
    /// 向指定用户的所有在线连接推送站内通知（离线静默，前端经 /api/notifications 轮询/重连补拉）。
    /// </summary>
    /// <param name="userId">目标用户（通知接收者）</param>
    /// <param name="dto">通知 DTO</param>
    public async Task NotifyNotificationAsync(Guid userId, NotificationDto dto, CancellationToken ct = default)
    {
        var connections = await GetOnlineConnectionsAsync([userId], ct);
        if (connections.Count == 0)
            return;

        await Task.WhenAll(
            connections.Select(c => _hubContext.Clients.Client(c).PushNotification(dto)));
    }

    /// <summary>
    /// 通知会话内其他用户某消息已被读取。
    /// </summary>
    public async Task NotifyMessageReadAsync(
        Guid sessionId, Guid messageId, Guid readerId,
        IEnumerable<Guid> participantIds, CancellationToken ct = default)
    {
        var others = participantIds.Where(p => p != readerId);
        var connections = await GetOnlineConnectionsAsync(others, ct);

        await Task.WhenAll(
            connections.Select(c => _hubContext.Clients.Client(c).MessageRead(messageId, readerId)));
    }

    /// <summary>
    /// 通知会话内所有参与者某消息已被撤回。
    /// </summary>
    public async Task NotifyMessageRecalledAsync(
        Guid sessionId, Guid messageId, IEnumerable<Guid> participantIds, CancellationToken ct = default)
    {
        var connections = await GetOnlineConnectionsAsync(participantIds, ct);

        await Task.WhenAll(
            connections.Select(c => _hubContext.Clients.Client(c).MessageRecalled(messageId)));
    }

    /// <summary>
    /// 向指定用户的全部在线连接推送未读消息数量更新。
    /// </summary>
    public async Task NotifyUnreadCountAsync(
        Guid sessionId, Guid userId, int count, CancellationToken ct = default)
    {
        await _hubContext.Clients.User(userId.ToString()).UnreadCountUpdated(sessionId, count);
    }

    /// <summary>
    /// 向指定用户的全部在线连接推送分片上传进度。
    /// </summary>
    public async Task PushUploadProgressAsync(
        Guid userId, ChunkUploadProgress progress, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        await _hubContext.Clients.User(userId.ToString()).UploadProgress(progress);
    }

    /// <summary>
    /// 向指定接收者集合推送用户上下线状态。
    /// </summary>
    /// <param name="userId">状态变化的目标用户</param>
    /// <param name="isOnline">是否上线</param>
    /// <param name="recipients">接收状态通知的用户列表（例如好友）；为空则不推送</param>
    public async Task NotifyUserStatusChangedAsync(
        Guid userId, bool isOnline, IEnumerable<Guid> recipients, CancellationToken ct = default)
    {
        var connections = await GetOnlineConnectionsAsync(recipients, ct);

        await Task.WhenAll(connections.Select(c =>
            isOnline
                ? _hubContext.Clients.Client(c).UserOnline(userId)
                : _hubContext.Clients.Client(c).UserOffline(userId)));
    }
}

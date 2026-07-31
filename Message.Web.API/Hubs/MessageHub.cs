using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Message.Web.API.Hubs;
namespace Message.Web.API.Hubs;


//[Authorize]
public class MessageHub : Hub<IMessageClient>
{
    private readonly IConnectionManager _connectionManager;
    private readonly ILogger<MessageHub> _logger;
    private readonly IMessageProvider _messageProvider;
    private readonly IChatSessionProvider _sessionProvider;

    public MessageHub(
        IMessageProvider messageProvider,
        IChatSessionProvider sessionProvider,
        IConnectionManager connectionManager,
        ILogger<MessageHub> logger)
    {
        _messageProvider = messageProvider;
        _sessionProvider = sessionProvider;
        _connectionManager = connectionManager;
        _logger = logger;
    }

    /// <summary>
    /// 当用户连接到Hub时调用
    /// </summary>
    [HubMethodName("OnConnected")]
    public override async Task OnConnectedAsync()
    {
        try
        {
            var userId = GetUserId();

            if (Context.User?.Identity?.IsAuthenticated != true)
            {
                throw new HubException("用户未认证");
            }

            var connectionId = Context.ConnectionId;

            await _connectionManager.AddConnectionAsync(userId, connectionId);
            await _connectionManager.SetUserOnlineAsync(userId);

            _logger.LogInformation("用户连接: {UserId}, ConnectionId: {ConnectionId}", userId, Context.ConnectionId);

            var offlineMessages = await _messageProvider.GetUnreadMessagesAsync(userId);
            foreach (var message in offlineMessages)
            {
                await Clients.Caller.ReceiveMessage(message.MapToDto());
            }

            await base.OnConnectedAsync();
        }
        catch (Exception ex) when (ex is not HubException)
        {
            _logger.LogError(ex, "用户连接时发生错误");
            throw;
        }
    }

    /// <summary>
    /// 当用户断开连接时调用
    /// </summary>
    /// <param name="exception">断开连接时发生的异常</param>
    [HubMethodName("OnDisconnected")]
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            var userId = GetUserId();
            var connectionId = Context.ConnectionId;

            await _connectionManager.RemoveConnectionAsync(userId, connectionId);

            var hasOtherConnections = await _connectionManager.HasOtherConnectionsAsync(userId);
            if (!hasOtherConnections)
            {
                await _connectionManager.SetUserOfflineAsync(userId);
            }

            _logger.LogInformation("用户 {UserId} 已断开连接，连接ID: {ConnectionId}", userId, connectionId);

            await base.OnDisconnectedAsync(exception);
        }
        catch (Exception ex) when (ex is not HubException)
        {
            _logger.LogError(ex, "用户断开连接时发生错误");
            throw;
        }
    }

    /// <summary>
    /// 发送消息到指定会话
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="request">消息请求</param>
    [HubMethodName("SendMessage")]
    public async Task SendMessage(Guid sessionId, SendMessageRequest request)
    {
        var userId = GetUserId();

        var message = request.MessageType switch
        {
            MessageType.MessageText => await _messageProvider.SendTextMessageAsync(sessionId, userId,
                request.Content ?? ""),
            MessageType.MessageImage => await _messageProvider.SendImageMessageAsync(sessionId, userId,
                new Uri(request.MediaUrl!), request.Caption, request.ThumbnailUrl),
            MessageType.MessageVideo => await _messageProvider.SendVideoMessageAsync(sessionId, userId,
                new Uri(request.MediaUrl!), request.Duration ?? 0, request.Caption, request.ThumbnailUrl),
            MessageType.MessageAudio => await _messageProvider.SendAudioMessageAsync(sessionId, userId,
                new Uri(request.MediaUrl!), request.Duration ?? 0, request.Caption),
            MessageType.MessageFile => await _messageProvider.SendFileMessageAsync(sessionId, userId,
                new Uri(request.MediaUrl!), request.FileName!, request.FileSize ?? 0, request.MimeType!),
            MessageType.MessageLocation => await _messageProvider.SendLocationMessageAsync(sessionId, userId,
                request.Latitude ?? 0, request.Longitude ?? 0, request.LocationName!),
            MessageType.MessageLink => await _messageProvider.SendLinkMessageAsync(sessionId, userId, request.LinkUrl!,
                request.LinkTitle, request.LinkDescription),
            MessageType.MessageExpression => await _messageProvider.SendExpressionMessageAsync(sessionId, userId,
                request.ExpressionCode!),
            _ => throw new NotSupportedException($"不支持的消息类型: {request.MessageType}")
        };

        var session = await _sessionProvider.GetSessionAsync(sessionId);
        if (session != null)
        {
            foreach (var participantId in session.Participants)
            {
                var connectionIds = await _connectionManager.GetConnectionsAsync(participantId);
                foreach (var connectionId in connectionIds)
                {
                    await Clients.Client(connectionId).ReceiveMessage(message.MapToDto());
                }
            }
        }
    }

    /// <summary>
    /// 标记消息为已读
    /// </summary>
    /// <param name="messageId">消息ID</param>
    [HubMethodName("MarkAsRead")]
    public async Task MarkAsRead(Guid messageId)
    {
        var userId = GetUserId();
        await _messageProvider.MarkAsReadAsync(messageId, userId);
        await Clients.Caller.MessageRead(messageId, userId);
    }

    /// <summary>
    /// 召回指定消息
    /// </summary>
    /// <param name="messageId">消息ID</param>
    [HubMethodName("RecallMessage")]
    public async Task RecallMessage(Guid messageId)
    {
        var userId = GetUserId();
        await _messageProvider.RecallMessageAsync(messageId, userId, RecallReason.UserRequest);
        await Clients.Caller.MessageRecalled(messageId);
    }

    /// <summary>
    /// 加入指定会话
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    [HubMethodName("JoinSession")]
    public async Task JoinSession(Guid sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"session:{sessionId}");
        _logger.LogDebug("连接 {ConnectionId} 加入会话 {SessionId}", Context.ConnectionId, sessionId);
    }

    /// <summary>
    /// 离开指定会话
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    [HubMethodName("LeaveSession")]
    public async Task LeaveSession(Guid sessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"session:{sessionId}");
        _logger.LogDebug("连接 {ConnectionId} 离开会话 {SessionId}", Context.ConnectionId, sessionId);
    }

    /// <summary>
    /// 发送正在输入指示
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    [HubMethodName("SendTypingIndicator")]
    public async Task SendTypingIndicator(Guid sessionId)
    {
        var userId = GetUserId();
        var session = await _sessionProvider.GetSessionAsync(sessionId);
        if (session != null)
        {
            foreach (var participantId in session.Participants.Where(p => p != userId))
            {
                var connectionIds = await _connectionManager.GetConnectionsAsync(participantId);
                foreach (var connectionId in connectionIds)
                {
                    await Clients.Client(connectionId).TypingIndicator(sessionId, userId);
                }
            }
        }
    }

    /// <summary>
    /// 获取当前连接的用户ID
    /// </summary>
    /// <returns>用户ID</returns>
    /// <exception cref="HubException">如果用户标识无效</exception>
    private Guid GetUserId()
    {
        var userIdClaim = Context.User?.FindFirst("sub")?.Value
                          ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(userIdClaim, out var userId)
            ? userId
            : throw new HubException("无效的用户标识");
    }
}


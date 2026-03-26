using System.Security.Claims;
using Message.Domain.Enums;
using Message.Domain.IProvider;
using Message.Domain.IServices;
using Message.Web.API.Dto.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Message.Web.API.Hubs;

[Authorize]
public class MessageHub(
    IMessageProvider messageProvider,
    IChatSessionProvider sessionProvider,
    IConnectionManager connectionManager,
    ILogger<MessageHub> logger)
    : Hub<IMessageClient>
{
    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        var connectionId = Context.ConnectionId;

        await connectionManager.AddConnectionAsync(userId, connectionId);
        await connectionManager.SetUserOnlineAsync(userId);

        logger.LogInformation("用户 {UserId} 已连接，连接ID: {ConnectionId}", userId, connectionId);

        var offlineMessages = await messageProvider.GetUnreadMessagesAsync(userId);
        foreach (var message in offlineMessages)
        {
            await Clients.Caller.ReceiveMessage(MapToDto(message));
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        var connectionId = Context.ConnectionId;

        await connectionManager.RemoveConnectionAsync(userId, connectionId);

        var hasOtherConnections = await connectionManager.HasOtherConnectionsAsync(userId);
        if (!hasOtherConnections)
        {
            await connectionManager.SetUserOfflineAsync(userId);
        }

        logger.LogInformation("用户 {UserId} 已断开连接，连接ID: {ConnectionId}", userId, connectionId);

        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(Guid sessionId, SendMessageRequest request)
    {
        var userId = GetUserId();

        var message = request.MessageType switch
        {
            MessageType.MessageText => await messageProvider.SendTextMessageAsync(sessionId, userId,
                request.Content ?? ""),
            MessageType.MessageImage => await messageProvider.SendImageMessageAsync(sessionId, userId,
                new Uri(request.MediaUrl!), request.Caption, request.ThumbnailUrl),
            MessageType.MessageVideo => await messageProvider.SendVideoMessageAsync(sessionId, userId,
                new Uri(request.MediaUrl!), request.Duration ?? 0, request.Caption, request.ThumbnailUrl),
            MessageType.MessageAudio => await messageProvider.SendAudioMessageAsync(sessionId, userId,
                new Uri(request.MediaUrl!), request.Duration ?? 0, request.Caption),
            MessageType.MessageFile => await messageProvider.SendFileMessageAsync(sessionId, userId,
                new Uri(request.MediaUrl!), request.FileName!, request.FileSize ?? 0, request.MimeType!),
            MessageType.MessageLocation => await messageProvider.SendLocationMessageAsync(sessionId, userId,
                request.Latitude ?? 0, request.Longitude ?? 0, request.LocationName!),
            MessageType.MessageLink => await messageProvider.SendLinkMessageAsync(sessionId, userId, request.LinkUrl!,
                request.LinkTitle, request.LinkDescription),
            MessageType.MessageExpression => await messageProvider.SendExpressionMessageAsync(sessionId, userId,
                request.ExpressionCode!),
            _ => throw new NotSupportedException($"不支持的消息类型: {request.MessageType}")
        };

        var session = await sessionProvider.GetSessionAsync(sessionId);
        if (session != null)
        {
            foreach (var participantId in session.Participants)
            {
                var connectionIds = await connectionManager.GetConnectionsAsync(participantId);
                foreach (var connectionId in connectionIds)
                {
                    await Clients.Client(connectionId).ReceiveMessage(MapToDto(message));
                }
            }
        }
    }

    public async Task MarkAsRead(Guid messageId)
    {
        var userId = GetUserId();
        await messageProvider.MarkAsReadAsync(messageId, userId);
        await Clients.Caller.MessageRead(messageId, userId);
    }

    public async Task RecallMessage(Guid messageId)
    {
        var userId = GetUserId();
        await messageProvider.RecallMessageAsync(messageId, userId, RecallReason.UserRequest);
        await Clients.Caller.MessageRecalled(messageId);
    }

    public async Task JoinSession(Guid sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"session:{sessionId}");
        logger.LogDebug("连接 {ConnectionId} 加入会话 {SessionId}", Context.ConnectionId, sessionId);
    }

    public async Task LeaveSession(Guid sessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"session:{sessionId}");
        logger.LogDebug("连接 {ConnectionId} 离开会话 {SessionId}", Context.ConnectionId, sessionId);
    }

    public async Task SendTypingIndicator(Guid sessionId)
    {
        var userId = GetUserId();
        var session = await sessionProvider.GetSessionAsync(sessionId);
        if (session != null)
        {
            foreach (var participantId in session.Participants.Where(p => p != userId))
            {
                var connectionIds = await connectionManager.GetConnectionsAsync(participantId);
                foreach (var connectionId in connectionIds)
                {
                    await Clients.Client(connectionId).TypingIndicator(sessionId, userId);
                }
            }
        }
    }

    private Guid GetUserId()
    {
        var userIdClaim = Context.User?.FindFirst("sub")?.Value
                          ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(userIdClaim, out var userId)
            ? userId
            : throw new HubException("无效的用户标识");
    }

    private static MessageDto MapToDto(Domain.Entities.Message message) => new()
    {
        MessageId = message.MessageId,
        SessionId = message.SessionId,
        SenderId = message.SenderId,
        ReceiverId = message.ReceiverId,
        MessageType = message.MessageType,
        Status = message.Status,
        Content = message.Content,
        MediaUrl = message.MediaUri?.ToString(),
        ThumbnailUrl = message.ThumbnailUri,
        FileName = message.FileName,
        FileSize = (long?)message.FileSize,
        MimeType = message.MimeType,
        Duration = message.Duration,
        Caption = message.Caption,
        Latitude = message.Latitude,
        Longitude = message.Longitude,
        LocationName = message.LocationName,
        LinkUrl = message.LinkUrl,
        LinkTitle = message.LinkTitle,
        LinkDescription = message.LinkDescription,
        ExpressionCode = message.ExpressionCode,
        SentTime = message.SentTime,
        DeliveredTime = message.DeliveredTime,
        ReadTime = message.ReadTime,
        IsRecalled = message.IsRecalled,
        IsForwarded = message.IsForwarded,
        OriginalMessageId = message.OriginalMessageId,
        ReplyToMessageId = message.ReplyToMessageId
    };
}

public interface IMessageClient
{
    Task ReceiveMessage(MessageDto message);
    Task MessageRecalled(Guid messageId);
    Task MessageRead(Guid messageId, Guid readerId);
    Task UserOnline(Guid userId);
    Task UserOffline(Guid userId);
    Task TypingIndicator(Guid sessionId, Guid userId);
    Task UnreadCountUpdated(Guid sessionId, int count);
}
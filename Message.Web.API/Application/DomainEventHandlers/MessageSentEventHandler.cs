using Message.Web.API.Hubs;
using Microsoft.AspNetCore.SignalR;
using NotMediator;

namespace Message.Web.API.Application.DomainEventHandlers;

public class MessageSentEventHandler : INotificationHandler<MessageSentEvent>
{
    private readonly IConnectionManager _connectionManager;
    private readonly IHubContext<MessageHub, IMessageClient> _hubContext;
    private readonly ILogger<MessageSentEventHandler> _logger;

    public MessageSentEventHandler(
        IConnectionManager connectionManager,
        IHubContext<MessageHub, IMessageClient> hubContext,
        ILogger<MessageSentEventHandler> logger)
    {
        _connectionManager = connectionManager;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Handler(MessageSentEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogDebug("处理消息发送事件: MessageId={MessageId}, SessionId={SessionId}",
            notification.MessageId, notification.SessionId);

        _logger.LogInformation("消息 {MessageId} 已发送到会话 {SessionId}",
            notification.MessageId, notification.SessionId);

        await Task.CompletedTask;
    }
}

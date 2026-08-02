using Message.Web.API.Hubs;
using Microsoft.AspNetCore.SignalR;
using NotMediator;

namespace Message.Web.API.Application.DomainEventHandlers;

public class MessageRecalledEventHandler : INotificationHandler<MessageRecalledEvent>
{
    private readonly IConnectionManager _connectionManager;
    private readonly IHubContext<MessageHub, IMessageClient> _hubContext;
    private readonly ILogger<MessageRecalledEventHandler> _logger;

    public MessageRecalledEventHandler(
        IConnectionManager connectionManager,
        IHubContext<MessageHub, IMessageClient> hubContext,
        ILogger<MessageRecalledEventHandler> logger)
    {
        _connectionManager = connectionManager;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Handler(MessageRecalledEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogDebug("处理消息撤回事件: MessageId={MessageId}, RecalledBy={RecalledBy}", 
            notification.MessageId, notification.RecalledBy);

        _logger.LogInformation("消息 {MessageId} 已被用户 {RecalledBy} 撤回，原因: {Reason}", 
            notification.MessageId, notification.RecalledBy, notification.Reason);

        await Task.CompletedTask;
    }
}

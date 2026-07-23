using NotMediator;

namespace Message.Web.API.Application.DomainEventHandlers;

public class MessageReadEventHandler : INotificationHandler<MessageReadEvent>
{
    private readonly IConnectionManager _connectionManager;
    private readonly IHubContext<Hub> _hubContext;
    private readonly ILogger<MessageReadEventHandler> _logger;

    public MessageReadEventHandler(
        IConnectionManager connectionManager,
        IHubContext<Hub> hubContext,
        ILogger<MessageReadEventHandler> logger)
    {
        _connectionManager = connectionManager;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Handler(MessageReadEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogDebug("处理消息已读事件: MessageId={MessageId}, ReaderId={ReaderId}", 
            notification.MessageId, notification.ReaderId);

        _logger.LogInformation("消息 {MessageId} 已被用户 {ReaderId} 于 {ReadTime} 读取", 
            notification.MessageId, notification.ReaderId, notification.ReadTime);

        await Task.CompletedTask;
    }
}

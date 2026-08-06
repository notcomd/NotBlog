
namespace Message.Web.API.Application.DomainEventHandlers;

public class MessageForwardedEventHandler : INotificationHandler<MessageForwardedEvent>
{
    private readonly ILogger<MessageForwardedEventHandler> _logger;

    public MessageForwardedEventHandler(ILogger<MessageForwardedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handler(MessageForwardedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogDebug("处理消息转发事件: OriginalMessageId={OriginalMessageId}", notification.OriginalMessageId);

        _logger.LogInformation("消息 {OriginalMessageId} 已转发为新消息 {ForwardedMessageId}，转发者: {ForwardedBy}，目标会话: {TargetSessionId}", 
            notification.OriginalMessageId, notification.ForwardedMessageId, notification.ForwardedBy, notification.TargetSessionId);

        await Task.CompletedTask;
    }
}


namespace Message.Web.API.Application.DomainEventHandlers;

public class SessionCreatedEventHandler : INotificationHandler<SessionCreatedEvent>
{
    private readonly ILogger<SessionCreatedEventHandler> _logger;

    public SessionCreatedEventHandler(ILogger<SessionCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handler(SessionCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogDebug("处理会话创建事件: SessionId={SessionId}", notification.SessionId);

        _logger.LogInformation("会话 {SessionId} 已创建，类型: {SessionType}，参与者数量: {ParticipantCount}", 
            notification.SessionId, notification.SessionType, notification.Participants.Count);

        await Task.CompletedTask;
    }
}

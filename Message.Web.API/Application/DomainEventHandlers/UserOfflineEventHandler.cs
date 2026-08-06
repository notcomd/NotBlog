
namespace Message.Web.API.Application.DomainEventHandlers;

public class UserOfflineEventHandler : INotificationHandler<UserOfflineEvent>
{
    private readonly IConnectionManager _connectionManager;
    private readonly IHubContext<MessageHub, IMessageClient> _hubContext;
    private readonly ILogger<UserOfflineEventHandler> _logger;

    public UserOfflineEventHandler(
        IConnectionManager connectionManager,
        IHubContext<MessageHub, IMessageClient> hubContext,
        ILogger<UserOfflineEventHandler> logger)
    {
        _connectionManager = connectionManager;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Handler(UserOfflineEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogDebug("处理用户离线事件: UserId={UserId}", notification.UserId);

        _logger.LogInformation("用户 {UserId} 于 {OfflineTime} 离线", 
            notification.UserId, notification.OfflineTime);

        await Task.CompletedTask;
    }
}

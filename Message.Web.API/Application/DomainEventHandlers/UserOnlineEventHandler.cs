using NotMediator;

namespace Message.Web.API.Application.DomainEventHandlers;

public class UserOnlineEventHandler : INotificationHandler<UserOnlineEvent>
{
    private readonly IConnectionManager _connectionManager;
    private readonly IHubContext<Hub> _hubContext;
    private readonly ILogger<UserOnlineEventHandler> _logger;

    public UserOnlineEventHandler(
        IConnectionManager connectionManager,
        IHubContext<Hub> hubContext,
        ILogger<UserOnlineEventHandler> logger)
    {
        _connectionManager = connectionManager;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Handler(UserOnlineEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogDebug("处理用户上线事件: UserId={UserId}", notification.UserId);

        _logger.LogInformation("用户 {UserId} 于 {OnlineTime} 上线", 
            notification.UserId, notification.OnlineTime);

        await Task.CompletedTask;
    }
}

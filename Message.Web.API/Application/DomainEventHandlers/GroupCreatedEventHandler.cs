using NotMediator;

namespace Message.Web.API.Application.DomainEventHandlers;

public class GroupCreatedEventHandler : INotificationHandler<GroupCreatedEvent>
{
    private readonly ILogger<GroupCreatedEventHandler> _logger;

    public GroupCreatedEventHandler(ILogger<GroupCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handler(GroupCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogDebug("处理群组创建事件: GroupId={GroupId}", notification.GroupId);

        _logger.LogInformation("群组 {GroupId} '{GroupName}' 已创建，群主: {OwnerId}",
            notification.GroupId, notification.GroupName, notification.OwnerId);

        await Task.CompletedTask;
    }
}
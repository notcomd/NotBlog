
namespace Message.Web.API.Application.DomainEventHandlers;

public class GroupMemberJoinedEventHandler : INotificationHandler<GroupMemberJoinedEvent>
{
    private readonly IConnectionManager _connectionManager;
    private readonly ILogger<GroupMemberJoinedEventHandler> _logger;

    public GroupMemberJoinedEventHandler(
        IConnectionManager connectionManager,
        ILogger<GroupMemberJoinedEventHandler> logger)
    {
        _connectionManager = connectionManager;
        _logger = logger;
    }

    public async Task Handler(GroupMemberJoinedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogDebug("处理群组成员加入事件: GroupId={GroupId}, UserId={UserId}", 
            notification.GroupId, notification.UserId);

        _logger.LogInformation("用户 {UserId} 加入群组 {GroupId}，角色: {Role}", 
            notification.UserId, notification.GroupId, notification.Role);

        await Task.CompletedTask;
    }
}

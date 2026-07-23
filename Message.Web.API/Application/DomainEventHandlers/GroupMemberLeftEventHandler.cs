using NotMediator;

namespace Message.Web.API.Application.DomainEventHandlers;

public class GroupMemberLeftEventHandler : INotificationHandler<GroupMemberLeftEvent>
{
    private readonly ILogger<GroupMemberLeftEventHandler> _logger;

    public GroupMemberLeftEventHandler(ILogger<GroupMemberLeftEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handler(GroupMemberLeftEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogDebug("处理群组成员离开事件: GroupId={GroupId}, UserId={UserId}", 
            notification.GroupId, notification.UserId);

        _logger.LogInformation("用户 {UserId} 离开群组 {GroupId}", 
            notification.UserId, notification.GroupId);

        await Task.CompletedTask;
    }
}

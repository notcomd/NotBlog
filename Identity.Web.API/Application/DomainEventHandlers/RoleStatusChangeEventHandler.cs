using Identity.Domain.Entities.RoleAggregate;
using Identity.Domain.Events;

namespace Identity.Web.API.Application.DomainEventHandlers;

/// <summary>
/// 角色状态变更事件处理器
/// 
/// 职责：
///   1. 记录角色状态变更日志
/// </summary>
public class RoleStatusChangeEventHandler : INotificationHandler<RoleStatusChangeDomainEvent>
{
    private readonly ILogger<RoleStatusChangeEventHandler> _logger;

    public RoleStatusChangeEventHandler(ILogger<RoleStatusChangeEventHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Handler(RoleStatusChangeDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[{Time}] 处理角色状态变更事件: RoleGuid={RoleGuid}, NewStatus={RoleStatus}",
            DateTime.UtcNow, notification.RoleGuid, notification.RoleStatus);

        try
        {
            _logger.LogInformation("[{Time}] 角色状态已变更: RoleGuid={RoleGuid}, RoleStatus={RoleStatus}",
                DateTime.UtcNow, notification.RoleGuid, notification.RoleStatus);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{Time}] 角色状态变更事件处理失败: RoleGuid={RoleGuid}",
                DateTime.UtcNow, notification.RoleGuid);
        }
    }
}

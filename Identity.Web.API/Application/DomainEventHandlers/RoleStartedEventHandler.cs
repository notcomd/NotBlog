using Identity.Domain.Entities.RoleAggregate;
using Identity.Domain.Events;

namespace Identity.Web.API.Application.DomainEventHandlers;

/// <summary>
/// 角色创建事件处理器
/// 
/// 职责：
///   1. 记录角色创建日志
/// </summary>
public class RoleStartedEventHandler : INotificationHandler<RoleStartedDomainEvent>
{
    private readonly ILogger<RoleStartedEventHandler> _logger;

    public RoleStartedEventHandler(ILogger<RoleStartedEventHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Handler(RoleStartedDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[{Time}] 处理角色创建事件: RoleGuid={RoleGuid}, RoleName={RoleName}, RoleCode={RoleCode}",
            DateTime.UtcNow, notification.Roles.RoleGuid, notification.Roles.RoleName, notification.Roles.RoleCode);

        try
        {
            _logger.LogInformation("[{Time}] 新角色已创建: RoleGuid={RoleGuid}, RoleName={RoleName}, RoleAuthority={RoleAuthority}",
                DateTime.UtcNow, notification.Roles.RoleGuid, notification.Roles.RoleName,
                notification.Roles.RoleAuthority);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{Time}] 角色创建事件处理失败: RoleGuid={RoleGuid}",
                DateTime.UtcNow, notification.Roles.RoleGuid);
        }
    }
}

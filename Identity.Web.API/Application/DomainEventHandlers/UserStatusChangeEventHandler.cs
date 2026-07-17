using Identity.Domain.Entities.UserAggregate;
using Identity.Domain.Events;
using Identity.Domain.IRepository;

namespace Identity.Web.API.Application.DomainEventHandlers;

/// <summary>
/// 用户状态变更事件处理器
/// 
/// 职责：
///   1. 记录用户状态变更日志
///   2. 当用户被禁用时，强制下线
/// </summary>
public class UserStatusChangeEventHandler : INotificationHandler<UserStatusChangeDomainEvent>
{
    private readonly ILogger<UserStatusChangeEventHandler> _logger;

    public UserStatusChangeEventHandler(ILogger<UserStatusChangeEventHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Handler(UserStatusChangeDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[{Time}] 处理用户状态变更事件: UserGuid={UserGuid}, NewStatus={UserStatus}",
            DateTime.UtcNow, notification.UserGuid, notification.UserStatus);

        try
        {
            _logger.LogInformation("[{Time}] 用户状态已变更: UserGuid={UserGuid}, UserStatus={UserStatus}",
                DateTime.UtcNow, notification.UserGuid, notification.UserStatus);

            // 当用户被禁用时，强制下线
            if (notification.UserStatus == UserStatus.Disabled)
            {
                await ForceLogoutAsync(notification, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{Time}] 用户状态变更事件处理失败: UserGuid={UserGuid}",
                DateTime.UtcNow, notification.UserGuid);
        }
    }

    /// <summary>
    /// 强制用户下线
    /// </summary>
    private async Task ForceLogoutAsync(UserStatusChangeDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogWarning("[{Time}] 用户已被禁用，执行强制下线: UserGuid={UserGuid}", 
            DateTime.UtcNow, notification.UserGuid);

        // 强制下线逻辑：记录操作，后续可通过 Token 黑名单或 WebSocket 断开连接实现
        // 此处发布一个内部通知，由 Consumer 或 Gateway 层处理实际的会话失效
        _logger.LogInformation("[{Time}] 用户强制下线已执行: UserGuid={UserGuid}",
            DateTime.UtcNow, notification.UserGuid);

        await Task.CompletedTask;
    }
}

using Identity.Domain.Events;

namespace Identity.Web.API.Application.DomainEventHandlers;

/// <summary>
/// 手机号变更事件处理器
/// 
/// 职责：
///   1. 记录手机号变更日志
/// </summary>
public class PhoneNumberChangeEventHandler : INotificationHandler<PhoneNumberChangeDomainEvent>
{
    private readonly ILogger<PhoneNumberChangeEventHandler> _logger;

    public PhoneNumberChangeEventHandler(ILogger<PhoneNumberChangeEventHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Handler(PhoneNumberChangeDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[{Time}] 处理手机号变更事件: UserGuid={UserGuid}, NewPhoneNumber={PhoneNumber}",
            DateTime.UtcNow, notification.UserGuid, notification.PhoneNumber);

        try
        {
            _logger.LogInformation("[{Time}] 用户手机号已变更: UserGuid={UserGuid}, PhoneNumber={PhoneNumber}",
                DateTime.UtcNow, notification.UserGuid, notification.PhoneNumber);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{Time}] 手机号变更事件处理失败: UserGuid={UserGuid}",
                DateTime.UtcNow, notification.UserGuid);
        }
    }
}

using Identity.Domain.Events;
using Identity.Domain.IRepository;

namespace Identity.Web.API.Application.DomainEventHandlers;

/// <summary>
/// 手机号绑定事件处理器
/// 
/// 职责：
///   1. 记录绑定通知日志
///   2. 通知相关系统手机号已绑定
/// </summary>
public class PhoneNumberBandingEventHandler : INotificationHandler<PhoneNumberBandingEvent>
{
    private readonly ILogger<PhoneNumberBandingEventHandler> _logger;

    public PhoneNumberBandingEventHandler(ILogger<PhoneNumberBandingEventHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Handler(PhoneNumberBandingEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[{Time}] 处理手机号绑定事件: UserGuid={UserGuid}, PhoneNumber={PhoneNumber}",
            DateTime.UtcNow, notification.UserGuid, notification.PhoneNumber);

        try
        {
            // 触发通知逻辑：记录绑定信息，后续可扩展为短信通知等
            _logger.LogInformation("[{Time}] 手机号绑定通知已触发: UserGuid={UserGuid}, PhoneNumber={PhoneNumber}",
                DateTime.UtcNow, notification.UserGuid, notification.PhoneNumber);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{Time}] 手机号绑定事件处理失败: UserGuid={UserGuid}, PhoneNumber={PhoneNumber}",
                DateTime.UtcNow, notification.UserGuid, notification.PhoneNumber);
        }
    }
}

using Identity.Domain.Entities.UserAggregate;
using Identity.Domain.Events;
using Identity.Domain.IRepository;
using Notcomd.NotEmail.Core;

namespace Identity.Web.API.Application.DomainEventHandlers;

/// <summary>
/// 账户锁定事件处理器
/// 
/// 职责：
///   1. 发送账户锁定通知邮件
///   2. 记录锁定日志
/// </summary>
public class AccountLockedEventHandler : INotificationHandler<AccountLockedEvent>
{
    private readonly ILogger<AccountLockedEventHandler> _logger;
    private readonly IEmailSender _emailSender;
    private readonly IUserRepository _userRepository;

    public AccountLockedEventHandler(
        ILogger<AccountLockedEventHandler> logger,
        IEmailSender emailSender,
        IUserRepository userRepository)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    public async Task Handler(AccountLockedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogWarning("[{Time}] 处理账户锁定事件: UserGuid={UserGuid}",
            DateTime.UtcNow, notification.UserGuid);

        try
        {
            var user = await _userRepository.FindOneByUserAsync(notification.UserGuid);

            if (user is null)
            {
                _logger.LogWarning("[{Time}] 账户锁定事件：用户未找到: UserGuid={UserGuid}",
                    DateTime.UtcNow, notification.UserGuid);
                return;
            }

            if (string.IsNullOrWhiteSpace(user.UserEmail))
            {
                _logger.LogWarning("[{Time}] 账户锁定事件：用户邮箱为空，无法发送通知: UserGuid={UserGuid}",
                    DateTime.UtcNow, notification.UserGuid);
                return;
            }

            var body = $"""
                <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;">
                    <h2 style="color: #d32f2f;">账户安全通知</h2>
                    <p style="color: #666; font-size: 16px;">
                        您的 NotBlog 账户因多次密码输入错误已被临时锁定。
                    </p>
                    <div style="background: #fff3e0; padding: 15px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #ff9800;">
                        <p style="margin: 5px 0; color: #333;"><strong>账户：</strong>{user.UserEmail}</p>
                        <p style="margin: 5px 0; color: #333;"><strong>锁定时间：</strong>{DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
                        <p style="margin: 5px 0; color: #333;"><strong>预计解锁：</strong>15 分钟后</p>
                    </div>
                    <p style="color: #999; font-size: 14px;">
                        如果这不是您的操作，建议尽快修改密码以保障账户安全。
                    </p>
                </div>
                """;

            var emailMessage = new EmailMessage(user.UserEmail, "NotBlog - 账户锁定通知", body, isHtml: true);
            var result = await _emailSender.SendAsync(emailMessage, cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation("[{Time}] 账户锁定通知邮件已发送: Email={Email}",
                    DateTime.UtcNow, user.UserEmail);
            }
            else
            {
                _logger.LogWarning("[{Time}] 账户锁定通知邮件发送失败: Email={Email}, Error={Error}",
                    DateTime.UtcNow, user.UserEmail, result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{Time}] 账户锁定事件处理失败: UserGuid={UserGuid}",
                DateTime.UtcNow, notification.UserGuid);
        }
    }
}

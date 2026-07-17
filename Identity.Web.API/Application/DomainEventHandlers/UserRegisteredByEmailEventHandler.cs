using Identity.Domain.Entities.RoleAggregate;
using Identity.Domain.Events;
using Notcomd.NotEmail.Core;

namespace Identity.Web.API.Application.DomainEventHandlers;

/// <summary>
/// 用户邮箱注册成功事件处理器
/// 
/// 职责：
///   1. 发送欢迎邮件
///   2. 初始化默认角色（确保 "User" 角色已分配给该用户）
/// </summary>
public class UserRegisteredByEmailEventHandler : INotificationHandler<UserStartedByEmailDomainEvent>
{
    private readonly ILogger<UserRegisteredByEmailEventHandler> _logger;
    private readonly IEmailSender _emailSender;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IUserRepository _userRepository;

    public UserRegisteredByEmailEventHandler(
        ILogger<UserRegisteredByEmailEventHandler> logger,
        IEmailSender emailSender,
        IUserRoleRepository userRoleRepository,
        IUserRepository userRepository)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
        _userRoleRepository = userRoleRepository ?? throw new ArgumentNullException(nameof(userRoleRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    public async Task Handler(UserStartedByEmailDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[{Time}] 处理用户注册事件: UserGuid={UserGuid}, Email={Email}",
            DateTime.UtcNow, notification.UserGuid, notification.UserEmail);

        try
        {
            // ── 1. 发送欢迎邮件 ──
            await SendWelcomeEmailAsync(notification, cancellationToken);

            // ── 2. 初始化默认角色 ──
            await EnsureDefaultRoleAsync(notification, cancellationToken);

            _logger.LogInformation("[{Time}] 用户注册事件处理完成: UserGuid={UserGuid}",
                DateTime.UtcNow, notification.UserGuid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{Time}] 用户注册事件处理失败: UserGuid={UserGuid}, Email={Email}",
                DateTime.UtcNow, notification.UserGuid, notification.UserEmail);
        }
    }

    /// <summary>
    /// 发送欢迎邮件
    /// </summary>
    private async Task SendWelcomeEmailAsync(UserStartedByEmailDomainEvent notification,
        CancellationToken cancellationToken)
    {
        var body = $"""
            <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;">
                <h2 style="color: #333;">欢迎加入 NotBlog！</h2>
                <p style="color: #666; font-size: 16px;">
                    感谢您注册 NotBlog 账号。您的账号已成功创建，现在可以开始探索我们的服务。
                </p>
                <div style="background: #f5f5f5; padding: 15px; border-radius: 8px; margin: 20px 0;">
                    <p style="margin: 5px 0; color: #333;"><strong>注册邮箱：</strong>{notification.UserEmail}</p>
                    <p style="margin: 5px 0; color: #333;"><strong>注册时间：</strong>{DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
                </div>
                <p style="color: #999; font-size: 14px;">
                    如果您未进行此操作，请忽略此邮件。
                </p>
            </div>
            """;

        var emailMessage = new EmailMessage(notification.UserEmail, "欢迎加入 NotBlog！", body, isHtml: true);

        var result = await _emailSender.SendAsync(emailMessage, cancellationToken);

        if (result.Success)
        {
            _logger.LogInformation("[{Time}] 欢迎邮件已发送至: {Email}", DateTime.UtcNow, notification.UserEmail);
        }
        else
        {
            _logger.LogWarning("[{Time}] 欢迎邮件发送失败: {Email}, Error: {Error}",
                DateTime.UtcNow, notification.UserEmail, result.ErrorMessage);
        }
    }

    /// <summary>
    /// 确保用户已分配默认角色（"User" 角色）
    /// </summary>
    private async ValueTask EnsureDefaultRoleAsync(UserStartedByEmailDomainEvent notification,
        CancellationToken cancellationToken)
    {
        // 查找或创建 "User" 默认角色
        Roles? defaultRole = null;
        try
        {
            defaultRole = await _userRoleRepository.FindByUserRoleAsync("User");
        }
        catch (ArgumentNullException)
        {
            // "User" 角色不存在，创建之
            _logger.LogInformation("[{Time}] 默认 'User' 角色不存在，正在创建...", DateTime.UtcNow);
        }

        if (defaultRole is null)
        {
            defaultRole = Roles.RoleFactory.CreateUserRole();
            await _userRoleRepository.AddByUserRoleAsync(defaultRole);
            await _userRoleRepository.UnitOfWork.SavaChangesAsync(cancellationToken);

            _logger.LogInformation("[{Time}] 默认 'User' 角色已创建: RoleGuid={RoleGuid}",
                DateTime.UtcNow, defaultRole.RoleGuid);
        }

        // 将角色分配给用户（如果尚未分配）
        var user = await _userRepository.FindOneByUserAsync(notification.UserEmail);
        if (user is null)
        {
            _logger.LogWarning("[{Time}] 注册用户未找到: Email={Email}", DateTime.UtcNow, notification.UserEmail);
            return;
        }

        if (!user.UserRoleGuid.Contains(defaultRole.RoleGuid))
        {
            user.UserRoleGuid.Add(defaultRole.RoleGuid);
            await _userRepository.UpdateByUserAsync(user);
            await _userRepository.UnitOfWork.SavaChangesAsync(cancellationToken);

            _logger.LogInformation("[{Time}] 用户 {UserGuid} 已分配到默认角色 '{RoleName}'",
                DateTime.UtcNow, user.UserGuid, defaultRole.RoleName);
        }
        else
        {
            _logger.LogDebug("[{Time}] 用户 {UserGuid} 已拥有默认角色，跳过分配",
                DateTime.UtcNow, user.UserGuid);
        }
    }
}

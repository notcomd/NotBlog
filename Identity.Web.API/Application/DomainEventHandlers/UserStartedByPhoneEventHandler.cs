using Identity.Domain.Entities.RoleAggregate;
using Identity.Domain.Events;
using Identity.Domain.IRepository;

namespace Identity.Web.API.Application.DomainEventHandlers;

/// <summary>
/// 用户手机号注册事件处理器
/// 
/// 职责：
///   1. 初始化默认角色（确保 "User" 角色已分配给该用户）
///   2. 记录注册日志
///   不发送欢迎短信
/// </summary>
public class UserStartedByPhoneEventHandler : INotificationHandler<UserStartedByPhoneDomainEvent>
{
    private readonly ILogger<UserStartedByPhoneEventHandler> _logger;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IUserRepository _userRepository;

    public UserStartedByPhoneEventHandler(
        ILogger<UserStartedByPhoneEventHandler> logger,
        IUserRoleRepository userRoleRepository,
        IUserRepository userRepository)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _userRoleRepository = userRoleRepository ?? throw new ArgumentNullException(nameof(userRoleRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    public async Task Handler(UserStartedByPhoneDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[{Time}] 处理用户手机号注册事件: UserGuid={UserGuid}, Phone={PhoneNumber}",
            DateTime.UtcNow, notification.UserGuid, notification.PhoneNumber.PhoneCode);

        try
        {
            // 初始化默认角色
            await EnsureDefaultRoleAsync(notification, cancellationToken);

            _logger.LogInformation("[{Time}] 用户手机号注册事件处理完成: UserGuid={UserGuid}",
                DateTime.UtcNow, notification.UserGuid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{Time}] 用户手机号注册事件处理失败: UserGuid={UserGuid}",
                DateTime.UtcNow, notification.UserGuid);
        }
    }

    /// <summary>
    /// 确保用户已分配默认角色（"User" 角色）
    /// </summary>
    private async ValueTask EnsureDefaultRoleAsync(UserStartedByPhoneDomainEvent notification,
        CancellationToken cancellationToken)
    {
        Roles? defaultRole = null;
        try
        {
            defaultRole = await _userRoleRepository.FindByUserRoleAsync("User");
        }
        catch (ArgumentNullException)
        {
            _logger.LogInformation("[{Time}] 默认 'User' 角色不存在，正在创建...", DateTime.UtcNow);
        }

        if (defaultRole is null)
        {
            defaultRole = Roles.RoleFactory.CreateUserRole();
            await _userRoleRepository.AddByUserRoleAsync(defaultRole);
            await _userRoleRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            _logger.LogInformation("[{Time}] 默认 'User' 角色已创建: RoleGuid={RoleGuid}",
                DateTime.UtcNow, defaultRole.RoleGuid);
        }

        var user = await _userRepository.FindOneByUserAsync(notification.UserGuid);
        if (user is null)
        {
            _logger.LogWarning("[{Time}] 注册用户未找到: UserGuid={UserGuid}", DateTime.UtcNow, notification.UserGuid);
            return;
        }

        if (!user.UserRoleGuid.Contains(defaultRole.RoleGuid))
        {
            user.UserRoleGuid.Add(defaultRole.RoleGuid);
            await _userRepository.UpdateByUserAsync(user);
            await _userRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

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

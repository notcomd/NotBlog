using Microsoft.Extensions.Options;

namespace NotBlog_Yarp.Permission;

/// <summary>
/// 配置驱动的权限服务客户端（开发/测试用）
/// 
/// 从 PermissionOptions.DevUsers 读取用户权限映射，
/// 不依赖 Identity 服务，适合本地开发和集成测试场景。
/// 使用 IOptionsSnapshot 支持配置热重载。
/// </summary>
public class ConfigPermissionServiceClient : IPermissionServiceClient
{
    private readonly IOptionsSnapshot<PermissionOptions> _options;
    private readonly ILogger<ConfigPermissionServiceClient> _logger;

    public ConfigPermissionServiceClient(
        IOptionsSnapshot<PermissionOptions> options,
        ILogger<ConfigPermissionServiceClient> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task<bool> CheckPermissionAsync(
        Guid userId, string permissionCode, CancellationToken ct = default)
    {
        if (userId == Guid.Empty || string.IsNullOrWhiteSpace(permissionCode))
            return Task.FromResult(false);

        var devUsers = _options.Value.DevUsers;
        if (devUsers is null || devUsers.Count == 0)
        {
            _logger.LogDebug(
                "[ConfigPermissionClient] DevUsers 未配置，返回 false UserId={UserId}", userId);
            return Task.FromResult(false);
        }

        if (devUsers.TryGetValue(userId.ToString(), out var userConfig))
        {
            var hasPermission = userConfig.Permissions.Contains(permissionCode);
            _logger.LogDebug(
                "[ConfigPermissionClient] UserId={UserId} Code={Code} Result={Result}",
                userId, permissionCode, hasPermission);
            return Task.FromResult(hasPermission);
        }

        _logger.LogDebug(
            "[ConfigPermissionClient] 未找到用户配置 UserId={UserId}", userId);
        return Task.FromResult(false);
    }

    /// <inheritdoc />
    public Task<string> GetDataScopeAsync(Guid userId, CancellationToken ct = default)
    {
        if (userId == Guid.Empty)
            return Task.FromResult("0|");

        var devUsers = _options.Value.DevUsers;
        if (devUsers is not null &&
            devUsers.TryGetValue(userId.ToString(), out var userConfig) &&
            !string.IsNullOrWhiteSpace(userConfig.DataScope))
        {
            _logger.LogDebug(
                "[ConfigPermissionClient] DataScope UserId={UserId} Scope={Scope}",
                userId, userConfig.DataScope);
            return Task.FromResult(userConfig.DataScope);
        }

        return Task.FromResult("0|");
    }
}

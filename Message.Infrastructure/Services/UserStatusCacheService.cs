using CacheMemory.Core;

namespace Message.Infrastructure.Services;

/// <summary>用户在线状态缓存服务，基于 Redis 缓存用户在线状态与在线用户集合（TTL 5 分钟）。</summary>
public class UserStatusCacheService
{
    private const string UserStatusPrefix = "message:user:status:";
    private const string OnlineUsersKey = "message:online:users";
    private static readonly TimeSpan StatusTtl = TimeSpan.FromMinutes(5);
    private readonly IRedisCacheService _cache;
    private readonly ILogger<UserStatusCacheService> _logger;

    /// <summary>初始化 <see cref="UserStatusCacheService"/> 实例。</summary>
    /// <param name="cache">Redis 缓存服务。</param>
    /// <param name="logger">日志记录器。</param>
    public UserStatusCacheService(
        IRedisCacheService cache,
        ILogger<UserStatusCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <summary>将用户状态缓存为在线，并加入在线用户集合。</summary>
    public async Task SetUserOnlineAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var key = $"{UserStatusPrefix}{userId}";
        var status = new UserStatus { IsOnline = true, LastOnlineTime = DateTime.UtcNow };

        await _cache.StringSetAsync(key, JsonSerializer.Serialize(status), StatusTtl, cancellationToken);
        await _cache.SetAddAsync(OnlineUsersKey, userId.ToString(), cancellationToken);

        _logger.LogDebug("用户 {UserId} 状态已缓存为在线", userId);
    }

    /// <summary>将用户状态缓存为离线，并从在线用户集合移除。</summary>
    public async Task SetUserOfflineAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var key = $"{UserStatusPrefix}{userId}";
        var status = new UserStatus { IsOnline = false, LastOnlineTime = DateTime.UtcNow };

        await _cache.StringSetAsync(key, JsonSerializer.Serialize(status), StatusTtl, cancellationToken);
        await _cache.SetRemoveAsync(OnlineUsersKey, userId.ToString(), cancellationToken);

        _logger.LogDebug("用户 {UserId} 状态已缓存为离线", userId);
    }

    /// <summary>判断指定用户当前是否在线。</summary>
    public async Task<bool> IsUserOnlineAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _cache.SetContainsAsync(OnlineUsersKey, userId.ToString(), cancellationToken);
    }

    /// <summary>获取指定用户缓存中的最后在线时间，缺失时返回 null。</summary>
    public async Task<DateTime?> GetLastOnlineTimeAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var key = $"{UserStatusPrefix}{userId}";
        var value = await _cache.StringGetAsync(key, cancellationToken);

        if (string.IsNullOrEmpty(value))
            return null;

        try
        {
            var status = JsonSerializer.Deserialize<UserStatus>(value);
            return status?.LastOnlineTime;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>获取当前在线用户数量。</summary>
    public async Task<int> GetOnlineUserCountAsync(CancellationToken cancellationToken = default)
    {
        var count = await _cache.SetLengthAsync(OnlineUsersKey, cancellationToken);
        return (int)count;
    }

    /// <summary>
    /// 获取 Redis 在线用户集合（message:online:users）。
    /// 返回原始字符串（用户 ID），由调用方按需解析。
    /// </summary>
    public async Task<IEnumerable<string>> GetOnlineUserIdsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _cache.SetMembersAsync(OnlineUsersKey, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "读取在线用户列表失败，返回空集合");
            return [];
        }
    }

    private record UserStatus
    {
        public bool IsOnline { get; init; }
        public DateTime LastOnlineTime { get; init; }
    }
}

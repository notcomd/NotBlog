using CacheMemory.Core;

namespace Message.Infrastructure.Services;

public class UserStatusCacheService
{
    private const string UserStatusPrefix = "message:user:status:";
    private const string OnlineUsersKey = "message:online:users";
    private static readonly TimeSpan StatusTtl = TimeSpan.FromMinutes(5);
    private readonly IRedisCacheService _cache;
    private readonly ILogger<UserStatusCacheService> _logger;

    public UserStatusCacheService(
        IRedisCacheService cache,
        ILogger<UserStatusCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task SetUserOnlineAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var key = $"{UserStatusPrefix}{userId}";
        var status = new UserStatus { IsOnline = true, LastOnlineTime = DateTime.UtcNow };

        await _cache.StringSetAsync(key, JsonSerializer.Serialize(status), StatusTtl, cancellationToken);
        await _cache.SetAddAsync(OnlineUsersKey, userId.ToString(), cancellationToken);

        _logger.LogDebug("用户 {UserId} 状态已缓存为在线", userId);
    }

    public async Task SetUserOfflineAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var key = $"{UserStatusPrefix}{userId}";
        var status = new UserStatus { IsOnline = false, LastOnlineTime = DateTime.UtcNow };

        await _cache.StringSetAsync(key, JsonSerializer.Serialize(status), StatusTtl, cancellationToken);
        await _cache.SetRemoveAsync(OnlineUsersKey, userId.ToString(), cancellationToken);

        _logger.LogDebug("用户 {UserId} 状态已缓存为离线", userId);
    }

    public async Task<bool> IsUserOnlineAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _cache.SetContainsAsync(OnlineUsersKey, userId.ToString(), cancellationToken);
    }

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

    public async Task<int> GetOnlineUserCountAsync(CancellationToken cancellationToken = default)
    {
        var count = await _cache.SetLengthAsync(OnlineUsersKey, cancellationToken);
        return (int)count;
    }

    private record UserStatus
    {
        public bool IsOnline { get; init; }
        public DateTime LastOnlineTime { get; init; }
    }
}

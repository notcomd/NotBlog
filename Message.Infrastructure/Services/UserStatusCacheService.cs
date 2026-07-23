using System.Text.Json;

namespace Message.Infrastructure.Services;

public class UserStatusCacheService 
{
    private const string UserStatusPrefix = "message:user:status:";
    private const string OnlineUsersKey = "message:online:users";
    private static readonly TimeSpan StatusTtl = TimeSpan.FromMinutes(5);
    private readonly IDatabase _database;
    private readonly ILogger<UserStatusCacheService> _logger;

    public UserStatusCacheService(
        IConnectionMultiplexer redis,
        ILogger<UserStatusCacheService> logger)
    {
        _database = redis.GetDatabase();
        _logger = logger;
    }

    public async Task SetUserOnlineAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var key = $"{UserStatusPrefix}{userId}";
        var status = new UserStatus { IsOnline = true, LastOnlineTime = DateTime.UtcNow };

        await _database.StringSetAsync(key, JsonSerializer.Serialize(status), StatusTtl);
        await _database.SetAddAsync(OnlineUsersKey, userId.ToString());

        _logger.LogDebug("用户 {UserId} 状态已缓存为在线", userId);
    }

    public async Task SetUserOfflineAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var key = $"{UserStatusPrefix}{userId}";
        var status = new UserStatus { IsOnline = false, LastOnlineTime = DateTime.UtcNow };

        await _database.StringSetAsync(key, JsonSerializer.Serialize(status), StatusTtl);
        await _database.SetRemoveAsync(OnlineUsersKey, userId.ToString());

        _logger.LogDebug("用户 {UserId} 状态已缓存为离线", userId);
    }

    public async Task<bool> IsUserOnlineAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _database.SetContainsAsync(OnlineUsersKey, userId.ToString());
    }

    public async Task<DateTime?> GetLastOnlineTimeAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var key = $"{UserStatusPrefix}{userId}";
        var value = await _database.StringGetAsync(key);

        if (value.IsNullOrEmpty)
            return null;

        try
        {
            var status = JsonSerializer.Deserialize<UserStatus>((string)value!);
            return status?.LastOnlineTime;
        }
        catch
        {
            return null;
        }
    }

    public async Task<int> GetOnlineUserCountAsync(CancellationToken cancellationToken = default)
    {
        var count = await _database.SetLengthAsync(OnlineUsersKey);
        return (int)count;
    }

    private record UserStatus
    {
        public bool IsOnline { get; init; }
        public DateTime LastOnlineTime { get; init; }
    }
}
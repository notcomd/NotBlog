using CacheMemory.Core;
using System.Text.Json;

namespace Message.Infrastructure.Services;

public class RedisConnectionManager : IConnectionManager, IConnectionCommandService
{
    private readonly string UserConnectionsPrefix = "message:user:";
    private readonly string ConnectionUserPrefix = "message:connection:";
    private readonly string UserStatusPrefix = "message:user:status:";
    private readonly string OnlineUsersKey = "message:online:users";

    private static readonly TimeSpan ConnectionTtl = TimeSpan.FromHours(24);
    private static readonly TimeSpan StatusTtl = TimeSpan.FromMinutes(5);

    private readonly IRedisCacheService _cache;
    private readonly ILogger<RedisConnectionManager> _logger;

    public RedisConnectionManager(
        IRedisCacheService cache,
        ILogger<RedisConnectionManager> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task AddConnectionAsync(Guid userId, string connectionId)
    {
        var userConnectionsKey = $"{UserConnectionsPrefix}{userId}:connections";
        var connectionUserKey = $"{ConnectionUserPrefix}{connectionId}:user";

        await _cache.SetAddAsync(userConnectionsKey, connectionId);
        await _cache.KeyExpireAsync(userConnectionsKey, ConnectionTtl);

        await _cache.StringSetAsync(
            connectionUserKey,
            userId.ToString(),
            ConnectionTtl);

        _logger.LogDebug(
            "用户 {UserId} 添加连接 {ConnectionId}",
            userId,
            connectionId);
    }

    public async Task RemoveConnectionAsync(Guid userId, string connectionId)
    {
        var userConnectionsKey = $"{UserConnectionsPrefix}{userId}:connections";
        var connectionUserKey = $"{ConnectionUserPrefix}{connectionId}:user";

        await _cache.SetRemoveAsync(userConnectionsKey, connectionId);
        await _cache.KeyDeleteAsync(connectionUserKey);

        var remainingConnections = await _cache.SetLengthAsync(userConnectionsKey);

        if (remainingConnections == 0)
        {
            await _cache.SetRemoveAsync(
                OnlineUsersKey,
                userId.ToString());
        }

        _logger.LogDebug(
            "用户 {UserId} 移除连接 {ConnectionId}",
            userId,
            connectionId);
    }

    public async Task<IEnumerable<string>> GetConnectionsAsync(Guid userId)
    {
        var key = $"{UserConnectionsPrefix}{userId}:connections";
        return await _cache.SetMembersAsync(key);
    }

    public async Task<bool> HasOtherConnectionsAsync(Guid userId)
    {
        var key = $"{UserConnectionsPrefix}{userId}:connections";
        return await _cache.SetLengthAsync(key) > 0;
    }

    public async Task SetUserOnlineAsync(Guid userId)
    {
        var statusKey = $"{UserStatusPrefix}{userId}";
        var status = new UserStatus
        {
            IsOnline = true,
            LastOnlineTime = DateTime.UtcNow
        };

        await _cache.StringSetAsync(
            statusKey,
            JsonSerializer.Serialize(status),
            StatusTtl);

        await _cache.SetAddAsync(
            OnlineUsersKey,
            userId.ToString());

        _logger.LogInformation("用户 {UserId} 已上线", userId);
    }

    public async Task SetUserOfflineAsync(Guid userId)
    {
        var statusKey = $"{UserStatusPrefix}{userId}";
        var status = new UserStatus
        {
            IsOnline = false,
            LastOnlineTime = DateTime.UtcNow
        };

        await _cache.StringSetAsync(
            statusKey,
            JsonSerializer.Serialize(status),
            StatusTtl);

        await _cache.SetRemoveAsync(
            OnlineUsersKey,
            userId.ToString());

        _logger.LogInformation("用户 {UserId} 已离线", userId);
    }

    public Task<bool> IsUserOnlineAsync(Guid userId)
    {
        return _cache.SetContainsAsync(
            OnlineUsersKey,
            userId.ToString());
    }

    public async Task<int> GetOnlineCountAsync()
    {
        var count = await _cache.SetLengthAsync(OnlineUsersKey);
        return checked((int)count);
    }

    private sealed record UserStatus
    {
        public bool IsOnline { get; init; }
        public DateTime LastOnlineTime { get; init; }
    }
}
using System.Text.Json;

namespace Message.Infrastructure.Services;

public class RedisConnectionManager : IConnectionManager
{
    private const string UserConnectionsPrefix = "message:user:";
    private const string ConnectionUserPrefix = "message:connection:";
    private const string UserStatusPrefix = "message:user:status:";
    private const string OnlineUsersKey = "message:online:users";

    private static readonly TimeSpan ConnectionTtl = TimeSpan.FromHours(24);
    private static readonly TimeSpan StatusTtl = TimeSpan.FromMinutes(5);
    private readonly IDatabase _database;
    private readonly ILogger<RedisConnectionManager> _logger;
    private readonly IConnectionMultiplexer _redis;

    public RedisConnectionManager(
        IConnectionMultiplexer redis,
        ILogger<RedisConnectionManager> logger)
    {
        _redis = redis;
        _database = redis.GetDatabase();
        _logger = logger;
    }

    public async Task AddConnectionAsync(Guid userId, string connectionId)
    {
        var userConnectionsKey = $"{UserConnectionsPrefix}{userId}:connections";
        var connectionUserKey = $"{ConnectionUserPrefix}{connectionId}:user";

        var tasks = new List<Task>
        {
            _database.SetAddAsync(userConnectionsKey, connectionId),
            _database.KeyExpireAsync(userConnectionsKey, ConnectionTtl),
            _database.StringSetAsync(connectionUserKey, userId.ToString()),
            _database.KeyExpireAsync(connectionUserKey, ConnectionTtl)
        };

        await Task.WhenAll(tasks);

        _logger.LogDebug("用户 {UserId} 添加连接 {ConnectionId}", userId, connectionId);
    }

    public async Task RemoveConnectionAsync(Guid userId, string connectionId)
    {
        var userConnectionsKey = $"{UserConnectionsPrefix}{userId}:connections";
        var connectionUserKey = $"{ConnectionUserPrefix}{connectionId}:user";

        var tasks = new List<Task>
        {
            _database.SetRemoveAsync(userConnectionsKey, connectionId),
            _database.KeyDeleteAsync(connectionUserKey)
        };

        await Task.WhenAll(tasks);

        var remainingConnections = await _database.SetLengthAsync(userConnectionsKey);
        if (remainingConnections == 0)
        {
            await _database.SetRemoveAsync(OnlineUsersKey, userId.ToString());
        }

        _logger.LogDebug("用户 {UserId} 移除连接 {ConnectionId}", userId, connectionId);
    }

    public async Task<IEnumerable<string>> GetConnectionsAsync(Guid userId)
    {
        var userConnectionsKey = $"{UserConnectionsPrefix}{userId}:connections";
        var connections = await _database.SetMembersAsync(userConnectionsKey);
        return connections.Select(c => c.ToString());
    }

    public async Task<bool> HasOtherConnectionsAsync(Guid userId)
    {
        var userConnectionsKey = $"{UserConnectionsPrefix}{userId}:connections";
        var count = await _database.SetLengthAsync(userConnectionsKey);
        return count > 0;
    }

    public async Task SetUserOnlineAsync(Guid userId)
    {
        var userStatusKey = $"{UserStatusPrefix}{userId}";
        var status = new UserStatus
        {
            IsOnline = true,
            LastOnlineTime = DateTime.UtcNow
        };

        await _database.StringSetAsync(
            userStatusKey,
            JsonSerializer.Serialize(status),
            StatusTtl);

        await _database.SetAddAsync(OnlineUsersKey, userId.ToString());

        _logger.LogInformation("用户 {UserId} 已上线", userId);
    }

    public async Task SetUserOfflineAsync(Guid userId)
    {
        var userStatusKey = $"{UserStatusPrefix}{userId}";
        var status = new UserStatus
        {
            IsOnline = false,
            LastOnlineTime = DateTime.UtcNow
        };

        await _database.StringSetAsync(
            userStatusKey,
            JsonSerializer.Serialize(status),
            StatusTtl);

        await _database.SetRemoveAsync(OnlineUsersKey, userId.ToString());

        _logger.LogInformation("用户 {UserId} 已离线", userId);
    }

    public async Task<bool> IsUserOnlineAsync(Guid userId)
    {
        return await _database.SetContainsAsync(OnlineUsersKey, userId.ToString());
    }

    public async Task<int> GetOnlineCountAsync()
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
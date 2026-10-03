using CacheMemory.Core;
using System.Text.Json;

namespace Message.Infrastructure.Services;

/// <summary>基于 Redis 的连接管理器，负责用户连接、在线状态与在线用户集合的维护（连接 TTL 24 小时、状态 TTL 5 分钟）。</summary>
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

    /// <summary>初始化 <see cref="RedisConnectionManager"/> 实例。</summary>
    /// <param name="cache">Redis 缓存服务。</param>
    /// <param name="logger">日志记录器。</param>
    public RedisConnectionManager(
        IRedisCacheService cache,
        ILogger<RedisConnectionManager> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>为用户添加一条连接记录（同时维护连接与用户的映射）。</summary>
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

    /// <summary>移除用户的指定连接；当用户不再有连接时将其从在线集合移除。</summary>
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

    /// <summary>获取指定用户的全部连接 ID。</summary>
    public async Task<IEnumerable<string>> GetConnectionsAsync(Guid userId)
    {
        var key = $"{UserConnectionsPrefix}{userId}:connections";
        return await _cache.SetMembersAsync(key);
    }

    /// <summary>判断指定用户是否仍存在其他连接。</summary>
    public async Task<bool> HasOtherConnectionsAsync(Guid userId)
    {
        var key = $"{UserConnectionsPrefix}{userId}:connections";
        return await _cache.SetLengthAsync(key) > 0;
    }

    /// <summary>将用户标记为在线并加入在线用户集合。</summary>
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

    /// <summary>将用户标记为离线并从在线用户集合移除。</summary>
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

    /// <summary>判断指定用户当前是否在线。</summary>
    public Task<bool> IsUserOnlineAsync(Guid userId)
    {
        return _cache.SetContainsAsync(
            OnlineUsersKey,
            userId.ToString());
    }

    /// <summary>获取当前在线用户数量。</summary>
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
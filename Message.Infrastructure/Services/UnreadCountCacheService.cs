using Message.Domain.IServices;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Message.Infrastructure.Services;

public class UnreadCountCacheService : IUnreadCountCacheService
{
    private const string UnreadCountPrefix = "message:user:unread:";
    private static readonly TimeSpan UnreadCountTtl = TimeSpan.FromHours(1);
    private readonly IDatabase _database;
    private readonly ILogger<UnreadCountCacheService> _logger;

    public UnreadCountCacheService(
        IConnectionMultiplexer redis,
        ILogger<UnreadCountCacheService> logger)
    {
        _database = redis.GetDatabase();
        _logger = logger;
    }

    public async Task IncrementUnreadCountAsync(Guid userId, Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var key = $"{UnreadCountPrefix}{userId}";
        await _database.HashIncrementAsync(key, sessionId.ToString());
        await _database.KeyExpireAsync(key, UnreadCountTtl);
        _logger.LogDebug("用户 {UserId} 会话 {SessionId} 未读数已增加", userId, sessionId);
    }

    public async Task DecrementUnreadCountAsync(Guid userId, Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var key = $"{UnreadCountPrefix}{userId}";
        var current = await _database.HashGetAsync(key, sessionId.ToString());

        if (!current.IsNullOrEmpty && long.Parse(current!) > 0)
        {
            await _database.HashDecrementAsync(key, sessionId.ToString());
            _logger.LogDebug("用户 {UserId} 会话 {SessionId} 未读数已减少", userId, sessionId);
        }
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var key = $"{UnreadCountPrefix}{userId}";
        var value = await _database.HashGetAsync(key, sessionId.ToString());

        if (value.IsNullOrEmpty)
            return 0;

        return (int)long.Parse(value!);
    }

    public async Task<Dictionary<Guid, int>> GetAllUnreadCountsAsync(Guid userId,
        CancellationToken cancellationToken = default)
    {
        var key = $"{UnreadCountPrefix}{userId}";
        var entries = await _database.HashGetAllAsync(key);

        var result = new Dictionary<Guid, int>();
        foreach (var entry in entries)
        {
            if (Guid.TryParse((string?)entry.Name, out var sessionId))
            {
                result[sessionId] = (int)long.Parse(entry.Value!);
            }
        }

        return result;
    }

    public async Task ClearUnreadCountAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        var key = $"{UnreadCountPrefix}{userId}";
        await _database.HashDeleteAsync(key, sessionId.ToString());
        _logger.LogDebug("用户 {UserId} 会话 {SessionId} 未读数已清零", userId, sessionId);
    }

    public async Task<int> GetTotalUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var key = $"{UnreadCountPrefix}{userId}";
        var entries = await _database.HashGetAllAsync(key);

        long total = 0;
        foreach (var entry in entries)
        {
            total += long.Parse(entry.Value!);
        }

        return (int)total;
    }
}
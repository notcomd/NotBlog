using System.Text.Json;
using Message.Domain.IServices;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Message.Infrastructure.Services;

public class SessionCacheService 
{
    private const string SessionPrefix = "message:session:";
    private static readonly TimeSpan SessionTtl = TimeSpan.FromMinutes(30);
    private readonly IDatabase _database;
    private readonly ILogger<SessionCacheService> _logger;

    public SessionCacheService(
        IConnectionMultiplexer redis,
        ILogger<SessionCacheService> logger)
    {
        _database = redis.GetDatabase();
        _logger = logger;
    }

    public async Task CacheSessionAsync(Guid sessionId, object sessionInfo,
        CancellationToken cancellationToken = default)
    {
        var key = $"{SessionPrefix}{sessionId}";
        await _database.StringSetAsync(key, JsonSerializer.Serialize(sessionInfo), SessionTtl);
        _logger.LogDebug("会话 {SessionId} 已缓存", sessionId);
    }

    public async Task<T?> GetSessionAsync<T>(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var key = $"{SessionPrefix}{sessionId}";
        var value = await _database.StringGetAsync(key);

        if (value.IsNullOrEmpty)
            return default;

        try
        {
            return JsonSerializer.Deserialize<T>((string)value!);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "反序列化会话缓存失败: SessionId={SessionId}", sessionId);
            return default;
        }
    }

    public async Task InvalidateSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var key = $"{SessionPrefix}{sessionId}";
        await _database.KeyDeleteAsync(key);
        _logger.LogDebug("会话 {SessionId} 缓存已失效", sessionId);
    }

    public async Task SetLastMessageAsync(Guid sessionId, Guid messageId, string content,
        CancellationToken cancellationToken = default)
    {
        var key = $"{SessionPrefix}{sessionId}:lastmessage";
        var lastMessage = new LastMessageInfo
        {
            MessageId = messageId,
            Content = content,
            Time = DateTime.UtcNow
        };
        await _database.StringSetAsync(key, JsonSerializer.Serialize(lastMessage), SessionTtl);
        _logger.LogDebug("会话 {SessionId} 最后消息已更新", sessionId);
    }

    private record LastMessageInfo
    {
        public Guid MessageId { get; init; }
        public string Content { get; init; } = string.Empty;
        public DateTime Time { get; init; }
    }
}
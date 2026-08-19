using CacheMemory.Core;

namespace Message.Infrastructure.Services;

public class SessionCacheService
{
    private const string SessionPrefix = "message:session:";
    private static readonly TimeSpan SessionTtl = TimeSpan.FromMinutes(30);
    private readonly IRedisCacheService _cache;
    private readonly ILogger<SessionCacheService> _logger;

    public SessionCacheService(
        IRedisCacheService cache,
        ILogger<SessionCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task CacheSessionAsync(Guid sessionId, object sessionInfo,
        CancellationToken cancellationToken = default)
    {
        var key = $"{SessionPrefix}{sessionId}";
        await _cache.StringSetAsync(key, JsonSerializer.Serialize(sessionInfo), SessionTtl, cancellationToken);
        _logger.LogDebug("会话 {SessionId} 已缓存", sessionId);
    }

    public async Task<T?> GetSessionAsync<T>(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var key = $"{SessionPrefix}{sessionId}";
        var value = await _cache.StringGetAsync(key, cancellationToken);

        if (string.IsNullOrEmpty(value))
            return default;

        try
        {
            return JsonSerializer.Deserialize<T>(value);
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
        await _cache.KeyDeleteAsync(key, cancellationToken);
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
        await _cache.StringSetAsync(key, JsonSerializer.Serialize(lastMessage), SessionTtl, cancellationToken);
        _logger.LogDebug("会话 {SessionId} 最后消息已更新", sessionId);
    }

    private record LastMessageInfo
    {
        public Guid MessageId { get; init; }
        public string Content { get; init; } = string.Empty;
        public DateTime Time { get; init; }
    }
}

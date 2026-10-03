namespace Message.Infrastructure.Services;

/// <summary>未读消息计数缓存服务，按用户维度以 Hash 结构缓存各会话未读数（TTL 1 小时）。</summary>
public class UnreadCountCacheService
{
    private const string UnreadCountPrefix = "message:user:unread:";
    private static readonly TimeSpan UnreadCountTtl = TimeSpan.FromHours(1);
    private readonly IRedisCacheService _database;
    private readonly ILogger<UnreadCountCacheService> _logger;

    /// <summary>初始化 <see cref="UnreadCountCacheService"/> 实例。</summary>
    /// <param name="database">Redis 缓存服务。</param>
    /// <param name="logger">日志记录器。</param>
    public UnreadCountCacheService(
        IRedisCacheService database,
        ILogger<UnreadCountCacheService> logger)
    {
        _database = database;
        _logger = logger;
    }

    /// <summary>增加指定用户在某会话的未读消息计数。</summary>
    public async Task IncrementUnreadCountAsync(Guid userId, Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var key = $"{UnreadCountPrefix}{userId}";
        await _database.HashIncrementAsync(key, sessionId.ToString(), 1, cancellationToken);
        await _database.KeyExpireAsync(key, UnreadCountTtl, cancellationToken);
        _logger.LogDebug("用户 {UserId} 会话 {SessionId} 未读数已增加", userId, sessionId);
    }

    /// <summary>减少指定用户在某会话的未读消息计数（计数为正时才递减）。</summary>
    public async Task DecrementUnreadCountAsync(Guid userId, Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var key = $"{UnreadCountPrefix}{userId}";
        var current = await _database.HashGetAsync(key, sessionId.ToString(), cancellationToken);

        if (!string.IsNullOrEmpty(current) && long.Parse(current) > 0)
        {
            await _database.HashIncrementAsync(key, sessionId.ToString(), -1, cancellationToken);
            _logger.LogDebug("用户 {UserId} 会话 {SessionId} 未读数已减少", userId, sessionId);
        }
    }

    /// <summary>获取指定用户在某会话的未读消息计数，缺失时返回 0。</summary>
    public async Task<int> GetUnreadCountAsync(Guid userId, Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var key = $"{UnreadCountPrefix}{userId}";
        var value = await _database.HashGetAsync(key, sessionId.ToString(), cancellationToken);

        if (string.IsNullOrEmpty(value))
            return 0;

        return (int)long.Parse(value!);
    }

    /// <summary>获取指定用户所有会话的未读计数字典。</summary>
    public async Task<Dictionary<Guid, int>> GetAllUnreadCountsAsync(Guid userId,
        CancellationToken cancellationToken = default)
    {
        var key = $"{UnreadCountPrefix}{userId}";
        var entries = await _database.HashGetAllAsync(key, cancellationToken);

        var result = new Dictionary<Guid, int>();
        foreach (var entry in entries)
        {
            if (Guid.TryParse(entry.Key, out var sessionId))
            {
                result[sessionId] = (int)long.Parse(entry.Value!);
            }
        }

        return result;
    }

    /// <summary>清零指定用户在某会话的未读消息计数。</summary>
    public async Task ClearUnreadCountAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        var key = $"{UnreadCountPrefix}{userId}";
        await _database.HashDeleteAsync(key, sessionId.ToString(),cancellationToken);
        _logger.LogDebug("用户 {UserId} 会话 {SessionId} 未读数已清零", userId, sessionId);
    }

    /// <summary>统计指定用户所有会话的未读消息总数。</summary>
    public async Task<int> GetTotalUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var key = $"{UnreadCountPrefix}{userId}";
        var entries = await _database.HashGetAllAsync(key, cancellationToken);

        long total = 0;
        foreach (var entry in entries)
        {
            total += long.Parse(entry.Value!);
        }

        return (int)total;
    }

    /// <summary>
    /// 尝试读取缓存的未读总数（Q-05：读路径先查缓存，miss 返回 null 由调用方回源 DB 并回填）。
    /// 使用独立的 <c>:total</c> 键，避免与按会话维度的 hash 结构混淆。
    /// </summary>
    public async Task<int?> TryGetTotalUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var totalKey = $"{UnreadCountPrefix}{userId}:total";
        var value = await _database.StringGetAsync(totalKey, cancellationToken);
        if (string.IsNullOrEmpty(value) || !int.TryParse(value, out var count))
            return null;
        return count;
    }

    /// <summary>
    /// 将未读总数写入缓存（Q-05：DB 回源后回填，TTL 与 hash 一致）。
    /// </summary>
    public async Task SetTotalUnreadCountAsync(Guid userId, int count, CancellationToken cancellationToken = default)
    {
        var totalKey = $"{UnreadCountPrefix}{userId}:total";
        await _database.StringSetAsync(totalKey, count.ToString(), UnreadCountTtl, cancellationToken);
        _logger.LogDebug("用户 {UserId} 未读总数已写入缓存：{Count}", userId, count);
    }

    /// <summary>
    /// 失效用户未读计数缓存（Q-05：采用"写时失效"保守策略——发送/已读等写路径直接删除缓存键，
    /// 读路径 miss 时回源 DB 重建，避免增量计数与 DB 漂移导致未读数不准确）。
    /// </summary>
    public async Task InvalidateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var key = $"{UnreadCountPrefix}{userId}";
        var totalKey = $"{UnreadCountPrefix}{userId}:total";
        await _database.KeyDeleteAsync(key, cancellationToken);
        await _database.KeyDeleteAsync(totalKey, cancellationToken);
        _logger.LogDebug("用户 {UserId} 未读计数缓存已失效", userId);
    }
}

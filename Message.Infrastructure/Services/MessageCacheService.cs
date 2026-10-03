
namespace Message.Infrastructure.Services;

/// <summary>消息模块的通用 Redis 缓存服务，提供字符串、计数、Hash 与 Set 等缓存读写能力。</summary>
public class MessageCacheService
{
    private readonly IRedisCacheService _database;
    private readonly ILogger<MessageCacheService> _logger;

    /// <summary>初始化 <see cref="MessageCacheService"/> 实例。</summary>
    /// <param name="database">Redis 缓存服务。</param>
    /// <param name="logger">日志记录器。</param>
    public MessageCacheService(IRedisCacheService database,
        ILogger<MessageCacheService> logger)
    {
        _database = database;
        _logger = logger;
    }

    /// <summary>读取缓存并反序列化为指定类型，缺失或反序列化失败时返回默认值。</summary>
    public async Task<T?> GetAsync<T>(string cacheKey, CancellationToken cancellationToken = default)
    {
        var value = await _database.StringGetAsync(cacheKey, cancellationToken);
        if (string.IsNullOrEmpty(value))
            return default;
        try
        {
            return JsonSerializer.Deserialize<T>(value);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "反序列化缓存值失败: Key={Key}", cacheKey);
            return default;
        }
    }

    /// <summary>将对象序列化后写入缓存，可指定过期时间。</summary>
    public async Task SetAsync<T>(string cacheKey, T value, TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(value);
        await _database.StringSetAsync(cacheKey, json, expiration, cancellationToken);
        _logger.LogDebug("缓存已设置: Key={Key}, Expiration={Expiration}", cacheKey, expiration);
    }

    /// <summary>删除指定缓存键。</summary>
    public async Task RemoveAsync(string cacheKey, CancellationToken cancellationToken = default)
    {
        await _database.KeyDeleteAsync(cacheKey, cancellationToken);
        _logger.LogDebug("缓存已删除: Key={Key}", cacheKey);
    }

    /// <summary>判断指定缓存键是否存在。</summary>
    public async Task<bool> ExistsAsync(string cacheKey, CancellationToken cancellationToken = default)
    {
        return await _database.KeyExistsAsync(cacheKey, cancellationToken);
    }

    /// <summary>对指定缓存键执行原子自增并返回自增后的值。</summary>
    public async Task<long> IncrementAsync(string cacheKey, long value = 1,
        CancellationToken cancellationToken = default)
    {
        return await _database.StringIncrementAsync(cacheKey, value, cancellationToken);
    }

    /// <summary>设置 Hash 缓存中指定字段的值。</summary>
    public async Task SetHashAsync(string cacheKey, string field, string value,
        CancellationToken cancellationToken = default)
    {
        await _database.HashSetAsync(cacheKey, field, value, cancellationToken);
        _logger.LogDebug("Hash缓存已设置: Key={Key}, Field={Field}", cacheKey, field);
    }

    /// <summary>读取 Hash 缓存中指定字段的值，不存在时返回 null。</summary>
    public async Task<string?> GetHashAsync(string cacheKey, string field,
        CancellationToken cancellationToken = default)
    {
        var value = await _database.HashGetAsync(cacheKey, field, cancellationToken);
        return string.IsNullOrEmpty(value) ? null : value;
    }

    /// <summary>向 Set 集合添加成员（原子去重），返回是否首次添加。</summary>
    public async Task<bool> SetAddAsync(string cacheKey, string value, CancellationToken cancellationToken = default)
    {
        return await _database.SetAddAsync(cacheKey, value, cancellationToken);
    }

    /// <summary>
    /// R-06：向 Set 添加成员（SADD 原子去重，返回是否首次添加）；新建 Set 时设置过期时间防止无界膨胀。
    /// </summary>
    public async Task<bool> SetAddAsync(string cacheKey, string value, TimeSpan? expiration,
        CancellationToken cancellationToken = default)
    {
        var added = await _database.SetAddAsync(cacheKey, value, cancellationToken);
        if (added && expiration is not null)
        {
            await _database.KeyExpireAsync(cacheKey, (TimeSpan)expiration, cancellationToken);
        }
        return added;
    }

    /// <summary>从 Set 集合移除成员，返回是否移除成功。</summary>
    public async Task<bool> SetRemoveAsync(string cacheKey, string value, CancellationToken cancellationToken = default)
    {
        return await _database.SetRemoveAsync(cacheKey, value, cancellationToken);
    }

    /// <summary>获取 Set 集合的全部成员。</summary>
    public async Task<IEnumerable<string>> SetMembersAsync(string cacheKey,
        CancellationToken cancellationToken = default)
    {
        return await _database.SetMembersAsync(cacheKey, cancellationToken);
    }
}

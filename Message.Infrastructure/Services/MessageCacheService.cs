
namespace Message.Infrastructure.Services;

public class MessageCacheService
{
    private readonly IRedisCacheService _database;
    private readonly ILogger<MessageCacheService> _logger;

    public MessageCacheService(IRedisCacheService database,
        ILogger<MessageCacheService> logger)
    {
        _database = database;
        _logger = logger;
    }

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

    public async Task SetAsync<T>(string cacheKey, T value, TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(value);
        await _database.StringSetAsync(cacheKey, json, expiration, cancellationToken);
        _logger.LogDebug("缓存已设置: Key={Key}, Expiration={Expiration}", cacheKey, expiration);
    }

    public async Task RemoveAsync(string cacheKey, CancellationToken cancellationToken = default)
    {
        await _database.KeyDeleteAsync(cacheKey, cancellationToken);
        _logger.LogDebug("缓存已删除: Key={Key}", cacheKey);
    }

    public async Task<bool> ExistsAsync(string cacheKey, CancellationToken cancellationToken = default)
    {
        return await _database.KeyExistsAsync(cacheKey, cancellationToken);
    }

    public async Task<long> IncrementAsync(string cacheKey, long value = 1,
        CancellationToken cancellationToken = default)
    {
        return await _database.StringIncrementAsync(cacheKey, value, cancellationToken);
    }

    public async Task SetHashAsync(string cacheKey, string field, string value,
        CancellationToken cancellationToken = default)
    {
        await _database.HashSetAsync(cacheKey, field, value, cancellationToken);
        _logger.LogDebug("Hash缓存已设置: Key={Key}, Field={Field}", cacheKey, field);
    }

    public async Task<string?> GetHashAsync(string cacheKey, string field,
        CancellationToken cancellationToken = default)
    {
        var value = await _database.HashGetAsync(cacheKey, field, cancellationToken);
        return string.IsNullOrEmpty(value) ? null : value;
    }

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

    public async Task<bool> SetRemoveAsync(string cacheKey, string value, CancellationToken cancellationToken = default)
    {
        return await _database.SetRemoveAsync(cacheKey, value, cancellationToken);
    }

    public async Task<IEnumerable<string>> SetMembersAsync(string cacheKey,
        CancellationToken cancellationToken = default)
    {
        return await _database.SetMembersAsync(cacheKey, cancellationToken);
    }
}

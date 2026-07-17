using System.Text.Json;
using Message.Domain.IServices;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Message.Infrastructure.Services;

public class RedisCacheService 
{
    private readonly IDatabase _database;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly IConnectionMultiplexer _redis;

    public RedisCacheService(
        IConnectionMultiplexer redis,
        ILogger<RedisCacheService> logger)
    {
        _redis = redis;
        _database = redis.GetDatabase();
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string cacheKey, CancellationToken cancellationToken = default)
    {
        var value = await _database.StringGetAsync(cacheKey);
        if (value.IsNullOrEmpty)
            return default;

        try
        {
            return JsonSerializer.Deserialize<T>((string)value!);
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
        await _database.StringSetAsync(cacheKey, json, expiration);
        _logger.LogDebug("缓存已设置: Key={Key}, Expiration={Expiration}", cacheKey, expiration);
    }

    public async Task RemoveAsync(string cacheKey, CancellationToken cancellationToken = default)
    {
        await _database.KeyDeleteAsync(cacheKey);
        _logger.LogDebug("缓存已删除: Key={Key}", cacheKey);
    }

    public async Task<bool> ExistsAsync(string cacheKey, CancellationToken cancellationToken = default)
    {
        return await _database.KeyExistsAsync(cacheKey);
    }

    public async Task<long> IncrementAsync(string cacheKey, long value = 1,
        CancellationToken cancellationToken = default)
    {
        return await _database.StringIncrementAsync(cacheKey, value);
    }

    public async Task SetHashAsync(string cacheKey, string field, string value,
        CancellationToken cancellationToken = default)
    {
        await _database.HashSetAsync(cacheKey, field, value);
        _logger.LogDebug("Hash缓存已设置: Key={Key}, Field={Field}", cacheKey, field);
    }

    public async Task<string?> GetHashAsync(string cacheKey, string field,
        CancellationToken cancellationToken = default)
    {
        var value = await _database.HashGetAsync(cacheKey, field);
        return value.IsNullOrEmpty ? null : value.ToString();
    }

    public async Task<bool> SetAddAsync(string cacheKey, string value, CancellationToken cancellationToken = default)
    {
        return await _database.SetAddAsync(cacheKey, value);
    }

    public async Task<bool> SetRemoveAsync(string cacheKey, string value, CancellationToken cancellationToken = default)
    {
        return await _database.SetRemoveAsync(cacheKey, value);
    }

    public async Task<IEnumerable<string>> SetMembersAsync(string cacheKey,
        CancellationToken cancellationToken = default)
    {
        var members = await _database.SetMembersAsync(cacheKey);
        return members.Select(m => m.ToString());
    }
}
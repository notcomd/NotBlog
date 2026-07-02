using System.Text.Json;
using CacheMemory.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CacheMemory.Service;

/// <summary>
/// 泛型缓存服务实现。
/// 为实现了 <see cref="IMemory"/> 标记接口的实体类型提供类型安全的缓存操作，
/// 自动处理 JSON 序列化/反序列化。
/// </summary>
/// <typeparam name="TMemory">实现了 <see cref="IMemory"/> 的实体类型</typeparam>
public class CacheMemory<TMemory> : ICacheMemory<TMemory> where TMemory : IMemory
{
    private readonly IRedisCacheService _cacheService;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<CacheMemory<TMemory>> _logger;

    public CacheMemory(
        IRedisCacheService cacheService,
        JsonSerializerOptions? jsonOptions = null,
        ILogger<CacheMemory<TMemory>>? logger = null)
    {
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _jsonOptions = jsonOptions ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);
        _logger = logger ?? NullLogger<CacheMemory<TMemory>>.Instance;
    }

    /// <inheritdoc />
    public virtual async Task<TMemory?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        var json = await _cacheService.StringGetAsync(key, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(json))
            return default;

        try
        {
            return JsonSerializer.Deserialize<TMemory>(json, _jsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "反序列化缓存实体失败，Key: {Key}", key);
            return default;
        }
    }

    /// <inheritdoc />
    public virtual async Task SetAsync(string key, TMemory value, TimeSpan? expiry = null,
        CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(value, _jsonOptions);
        await _cacheService.StringSetAsync(key, json, expiry, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual async Task<bool> SetIfNotExistsAsync(string key, TMemory value, TimeSpan? expiry = null,
        CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(value, _jsonOptions);
        return await _cacheService.StringSetIfNotExistsAsync(key, json, expiry, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual async Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
        => await _cacheService.KeyDeleteAsync(key, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public virtual async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        => await _cacheService.KeyExistsAsync(key, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public virtual async Task<bool> RefreshAsync(string key, TimeSpan expiry,
        CancellationToken cancellationToken = default)
        => await _cacheService.KeyExpireAsync(key, expiry, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public virtual async Task<IEnumerable<TMemory?>> GetManyAsync(IEnumerable<string> keys,
        CancellationToken cancellationToken = default)
    {
        var results = await _cacheService.StringGetManyAsync(keys, cancellationToken).ConfigureAwait(false);
        return results.Select(json =>
        {
            if (string.IsNullOrEmpty(json))
                return default;
            try
            {
                return JsonSerializer.Deserialize<TMemory>(json, _jsonOptions);
            }
            catch (JsonException)
            {
                return default;
            }
        });
    }

    /// <inheritdoc />
    public virtual async Task SetManyAsync(IDictionary<string, TMemory> entries, TimeSpan? expiry = null,
        CancellationToken cancellationToken = default)
    {
        var stringEntries = new Dictionary<string, string>();
        foreach (var (key, value) in entries)
        {
            stringEntries[key] = JsonSerializer.Serialize(value, _jsonOptions);
        }

        await _cacheService.StringSetManyAsync(stringEntries, expiry, cancellationToken).ConfigureAwait(false);
    }

    // =========================================================================
    // Sync Methods
    // =========================================================================

    /// <inheritdoc />
    public virtual TMemory? Get(string key)
    {
        var json = _cacheService.StringGet(key);
        if (string.IsNullOrEmpty(json))
            return default;

        try
        {
            return JsonSerializer.Deserialize<TMemory>(json, _jsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "同步反序列化缓存实体失败，Key: {Key}", key);
            return default;
        }
    }

    /// <inheritdoc />
    public virtual void Set(string key, TMemory value, TimeSpan? expiry = null)
    {
        var json = JsonSerializer.Serialize(value, _jsonOptions);
        _cacheService.StringSet(key, json, expiry);
    }

    /// <inheritdoc />
    public virtual bool Remove(string key)
        => _cacheService.KeyDelete(key);

    /// <summary>
    /// 使用实体的 CacheKey 获取缓存。
    /// 如果实体实现了 CacheKey 属性，可直接调用此方法无需手动指定键。
    /// </summary>
    public virtual async Task<TMemory?> GetByEntityAsync(TMemory entity, CancellationToken ct = default)
    {
        var key = entity.CacheKey ?? throw new InvalidOperationException(
            $"类型 {typeof(TMemory).Name} 未提供 CacheKey。请设置 CacheKey 属性或使用带 key 参数的重载。");
        return await GetAsync(key, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// 使用实体的 CacheKey 设置缓存。
    /// </summary>
    public virtual async Task SetByEntityAsync(TMemory entity, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        var key = entity.CacheKey ?? throw new InvalidOperationException(
            $"类型 {typeof(TMemory).Name} 未提供 CacheKey。请设置 CacheKey 属性或使用带 key 参数的重载。");
        await SetAsync(key, entity, expiry, ct).ConfigureAwait(false);
    }
}
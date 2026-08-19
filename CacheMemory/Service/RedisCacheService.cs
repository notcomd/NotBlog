using System.Data.Common;
using CacheMemory.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using StackExchange.Redis;

namespace CacheMemory.Service;

/// <summary>
/// Redis 缓存服务的完整实现。
/// 提供对 Redis 所有数据类型（String、Hash、List、Set、SortedSet、HyperLogLog、Bitmap、Geo）
/// 的完整操作支持，内置重试策略、异常处理与日志记录。
/// 所有方法均为 virtual，允许子类覆写以扩展或修改特定行为。
/// </summary>
public class RedisCacheService : IRedisCacheService
{
    private readonly IRedisConnectionProvider _connectionProvider;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly IRedisRetryPolicy _retryPolicy;

    public RedisCacheService(
        IRedisConnectionProvider connectionProvider,
        IRedisRetryPolicy retryPolicy,
        ILogger<RedisCacheService>? logger = null)
    {
        _connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
        _retryPolicy = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));
        _logger = logger ?? NullLogger<RedisCacheService>.Instance;
    }

    // =========================================================================
    // Key Management
    // =========================================================================

    public virtual async Task<bool> KeyExistsAsync(string key, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            return await db.KeyExistsAsync(key).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual bool KeyExists(string key)
        => _retryPolicy.Execute(() => GetDatabase().KeyExists(key));

    public virtual async Task<bool> KeyDeleteAsync(string key, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            return await db.KeyDeleteAsync(key).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual bool KeyDelete(string key)
        => _retryPolicy.Execute(() => GetDatabase().KeyDelete(key));

    public virtual async Task<long> KeyDeleteManyAsync(IEnumerable<string> keys, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            var redisKeys = keys.Select(k => (RedisKey)k).ToArray();
            return await db.KeyDeleteAsync(redisKeys).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual long KeyDeleteMany(IEnumerable<string> keys)
        => _retryPolicy.Execute(() =>
        {
            var redisKeys = keys.Select(k => (RedisKey)k).ToArray();
            return GetDatabase().KeyDelete(redisKeys);
        });

    public virtual async Task<bool> KeyExpireAsync(string key, TimeSpan expiry, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            return await db.KeyExpireAsync(key, expiry).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual bool KeyExpire(string key, TimeSpan expiry)
        => _retryPolicy.Execute(() => GetDatabase().KeyExpire(key, expiry));

    public virtual async Task<bool> KeyPersistAsync(string key, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            return await db.KeyPersistAsync(key).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual bool KeyPersist(string key)
        => _retryPolicy.Execute(() => GetDatabase().KeyPersist(key));

    public virtual async Task<TimeSpan?> KeyTtlAsync(string key, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            return await db.KeyTimeToLiveAsync(key).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual TimeSpan? KeyTtl(string key)
        => _retryPolicy.Execute(() => GetDatabase().KeyTimeToLive(key));

    public virtual async Task<RedisType> KeyTypeAsync(string key, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            return await db.KeyTypeAsync(key).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual RedisType KeyType(string key)
        => _retryPolicy.Execute(() => GetDatabase().KeyType(key));

    public virtual async Task<bool> KeyRenameAsync(string key, string newKey, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            return await db.KeyRenameAsync(key, newKey).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual bool KeyRename(string key, string newKey)
        => _retryPolicy.Execute(() => GetDatabase().KeyRename(key, newKey));

    public virtual async Task<IEnumerable<string>> KeysByPatternAsync(string pattern, int pageSize = 1000,
        CancellationToken ct = default)
    {
        var keys = new List<string>();
        var server = _connectionProvider.GetConnection().GetServers().FirstOrDefault()
                     ?? throw new InvalidOperationException("没有可用的 Redis 服务器");

        await foreach (var key in server.KeysAsync(pattern: pattern, pageSize: pageSize).WithCancellation(ct)
                           .ConfigureAwait(false))
        {
            keys.Add(key!);
        }

        return keys;
    }

    public virtual IEnumerable<string> KeysByPattern(string pattern, int pageSize = 1000)
    {
        var server = _connectionProvider.GetConnection().GetServers().FirstOrDefault()
                     ?? throw new InvalidOperationException("没有可用的 Redis 服务器");
        return server.Keys(pattern: pattern, pageSize: pageSize).Select(k => (string)k!);
    }

    // =========================================================================
    // String Operations
    // =========================================================================

    public virtual async Task<string?> StringGetAsync(string key, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            var result = await db.StringGetAsync(key).ConfigureAwait(false);
            return result.HasValue ? (string?)result : null;
        }, ct).ConfigureAwait(false);

    public virtual string? StringGet(string key)
        => _retryPolicy.Execute(() =>
        {
            var result = GetDatabase().StringGet(key);
            return result.HasValue ? (string?)result : null;
        });

    public virtual async Task<bool> StringSetAsync(string key, string value, TimeSpan? expiry = null,
        CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            return await db
                .StringSetAsync(key, value, (Expiration?)expiry ?? Expiration.Default, flags: CommandFlags.None)
                .ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual bool StringSet(string key, string value, TimeSpan? expiry = null)
        => _retryPolicy.Execute(() =>
            GetDatabase().StringSet(key, value, (Expiration?)expiry ?? Expiration.Default, flags: CommandFlags.None));

    public virtual async Task<bool> StringSetIfNotExistsAsync(string key, string value, TimeSpan? expiry = null,
        CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            return await db
                .StringSetAsync(key, value, (Expiration?)expiry ?? Expiration.Default, When.NotExists,
                    CommandFlags.None).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual bool StringSetIfNotExists(string key, string value, TimeSpan? expiry = null)
        => _retryPolicy.Execute(() => GetDatabase().StringSet(key, value, (Expiration?)expiry ?? Expiration.Default,
            When.NotExists, CommandFlags.None));

    public virtual async Task<long> StringIncrementAsync(string key, long value = 1, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            return await db.StringIncrementAsync(key, value).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual long StringIncrement(string key, long value = 1)
        => _retryPolicy.Execute(() => GetDatabase().StringIncrement(key, value));

    public virtual async Task<double> StringIncrementFloatAsync(string key, double value,
        CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            return await db.StringIncrementAsync(key, value).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual double StringIncrementFloat(string key, double value)
        => _retryPolicy.Execute(() => GetDatabase().StringIncrement(key, value));

    public virtual async Task<long> StringDecrementAsync(string key, long value = 1, CancellationToken ct = default)
        => await StringIncrementAsync(key, -value, ct).ConfigureAwait(false);

    public virtual long StringDecrement(string key, long value = 1)
        => StringIncrement(key, -value);

    public virtual async Task<long> StringAppendAsync(string key, string value, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            return await db.StringAppendAsync(key, value).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual long StringAppend(string key, string value)
        => _retryPolicy.Execute(() => GetDatabase().StringAppend(key, value));

    public virtual async Task<long> StringLengthAsync(string key, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            return await db.StringLengthAsync(key).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual long StringLength(string key)
        => _retryPolicy.Execute(() => GetDatabase().StringLength(key));

    public virtual async Task<string?[]> StringGetManyAsync(IEnumerable<string> keys, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            var redisKeys = keys.Select(k => (RedisKey)k).ToArray();
            var results = await db.StringGetAsync(redisKeys).ConfigureAwait(false);
            return results.Select(r => r.HasValue ? (string?)r : null).ToArray();
        }, ct).ConfigureAwait(false);

    public virtual string?[] StringGetMany(IEnumerable<string> keys)
        => _retryPolicy.Execute(() =>
        {
            var redisKeys = keys.Select(k => (RedisKey)k).ToArray();
            var results = GetDatabase().StringGet(redisKeys);
            return results.Select(r => r.HasValue ? (string?)r : null).ToArray();
        });

    public virtual async Task<bool> StringSetManyAsync(IDictionary<string, string> entries, TimeSpan? expiry = null,
        CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            var pairs = entries.Select(e => new KeyValuePair<RedisKey, RedisValue>(e.Key, e.Value)).ToArray();
            return await db
                .StringSetAsync(pairs, When.Always, (Expiration?)expiry ?? Expiration.Default, CommandFlags.None)
                .ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual bool StringSetMany(IDictionary<string, string> entries, TimeSpan? expiry = null)
        => _retryPolicy.Execute(() =>
        {
            var pairs = entries.Select(e => new KeyValuePair<RedisKey, RedisValue>(e.Key, e.Value)).ToArray();
            return GetDatabase().StringSet(pairs, When.Always, (Expiration?)expiry ?? Expiration.Default,
                CommandFlags.None);
        });

    // =========================================================================
    // Hash Operations
    // =========================================================================

    public virtual async Task<bool> HashSetAsync(string key, string field, string value, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            return await db.HashSetAsync(key, field, value).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual bool HashSet(string key, string field, string value)
        => _retryPolicy.Execute(() => GetDatabase().HashSet(key, field, value));

    public virtual async Task<string?> HashGetAsync(string key, string field, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            var result = await db.HashGetAsync(key, field).ConfigureAwait(false);
            return result.HasValue ? (string?)result : null;
        }, ct).ConfigureAwait(false);

    public virtual string? HashGet(string key, string field)
        => _retryPolicy.Execute(() =>
        {
            var result = GetDatabase().HashGet(key, field);
            return result.HasValue ? (string?)result : null;
        });

    public virtual async Task<bool> HashDeleteAsync(string key, string field, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            return await db.HashDeleteAsync(key, field).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual bool HashDelete(string key, string field)
        => _retryPolicy.Execute(() => GetDatabase().HashDelete(key, field));

    public virtual async Task<long> HashDeleteManyAsync(string key, IEnumerable<string> fields,
        CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            var redisFields = fields.Select(f => (RedisValue)f).ToArray();
            return await db.HashDeleteAsync(key, redisFields).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual long HashDeleteMany(string key, IEnumerable<string> fields)
        => _retryPolicy.Execute(() =>
        {
            var redisFields = fields.Select(f => (RedisValue)f).ToArray();
            return GetDatabase().HashDelete(key, redisFields);
        });

    public virtual async Task<bool> HashExistsAsync(string key, string field, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            return await db.HashExistsAsync(key, field).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual bool HashExists(string key, string field)
        => _retryPolicy.Execute(() => GetDatabase().HashExists(key, field));

    public virtual async Task<Dictionary<string, string>> HashGetAllAsync(string key, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            var entries = await db.HashGetAllAsync(key).ConfigureAwait(false);
            return entries.ToDictionary(e => (string)e.Name!, e => (string)e.Value!);
        }, ct).ConfigureAwait(false);

    public virtual Dictionary<string, string> HashGetAll(string key)
        => _retryPolicy.Execute(() =>
        {
            var entries = GetDatabase().HashGetAll(key);
            return entries.ToDictionary(e => (string)e.Name!, e => (string)e.Value!);
        });

    public virtual async Task<IEnumerable<string>> HashKeysAsync(string key, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            var keys = await db.HashKeysAsync(key).ConfigureAwait(false);
            return keys.Select(k => (string)k!).ToList();
        }, ct).ConfigureAwait(false);

    public virtual IEnumerable<string> HashKeys(string key)
        => _retryPolicy.Execute(() => GetDatabase().HashKeys(key).Select(k => (string)k!));

    public virtual async Task<IEnumerable<string>> HashValuesAsync(string key, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            var values = await db.HashValuesAsync(key).ConfigureAwait(false);
            return values.Select(v => (string)v!).ToList();
        }, ct).ConfigureAwait(false);

    public virtual IEnumerable<string> HashValues(string key)
        => _retryPolicy.Execute(() => GetDatabase().HashValues(key).Select(v => (string)v!));

    public virtual async Task<long> HashLengthAsync(string key, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            return await db.HashLengthAsync(key).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual long HashLength(string key)
        => _retryPolicy.Execute(() => GetDatabase().HashLength(key));

    public virtual async Task<long> HashIncrementAsync(string key, string field, long value = 1,
        CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            return await db.HashIncrementAsync(key, field, value).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual long HashIncrement(string key, string field, long value = 1)
        => _retryPolicy.Execute(() => GetDatabase().HashIncrement(key, field, value));

    public virtual async Task<double> HashIncrementFloatAsync(string key, string field, double value,
        CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            return await db.HashIncrementAsync(key, field, value).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual double HashIncrementFloat(string key, string field, double value)
        => _retryPolicy.Execute(() => GetDatabase().HashIncrement(key, field, value));

    public virtual async Task<IEnumerable<string?>> HashGetManyAsync(string key, IEnumerable<string> fields,
        CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            var redisFields = fields.Select(f => (RedisValue)f).ToArray();
            var results = await db.HashGetAsync(key, redisFields).ConfigureAwait(false);
            return results.Select(r => r.HasValue ? (string?)r : null);
        }, ct).ConfigureAwait(false);

    public virtual IEnumerable<string?> HashGetMany(string key, IEnumerable<string> fields)
        => _retryPolicy.Execute(() =>
        {
            var redisFields = fields.Select(f => (RedisValue)f).ToArray();
            var results = GetDatabase().HashGet(key, redisFields);
            return results.Select(r => r.HasValue ? (string?)r : null);
        });

    public virtual async Task HashSetManyAsync(string key, IDictionary<string, string> entries,
        CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            var hashEntries = entries.Select(e => new HashEntry(e.Key, e.Value)).ToArray();
            await db.HashSetAsync(key, hashEntries).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual void HashSetMany(string key, IDictionary<string, string> entries)
        => _retryPolicy.Execute(() =>
        {
            var hashEntries = entries.Select(e => new HashEntry(e.Key, e.Value)).ToArray();
            GetDatabase().HashSet(key, hashEntries);
        });

    // =========================================================================
    // List Operations
    // =========================================================================

    public virtual async Task<long> ListLeftPushAsync(string key, string value, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            return await db.ListLeftPushAsync(key, value).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual long ListLeftPush(string key, string value)
        => _retryPolicy.Execute(() => GetDatabase().ListLeftPush(key, value));

    public virtual async Task<long> ListLeftPushManyAsync(string key, IEnumerable<string> values,
        CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            var redisValues = values.Select(v => (RedisValue)v).ToArray();
            return await db.ListLeftPushAsync(key, redisValues).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual long ListLeftPushMany(string key, IEnumerable<string> values)
        => _retryPolicy.Execute(() =>
        {
            var redisValues = values.Select(v => (RedisValue)v).ToArray();
            return GetDatabase().ListLeftPush(key, redisValues);
        });

    public virtual async Task<long> ListRightPushAsync(string key, string value, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            return await db.ListRightPushAsync(key, value).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual long ListRightPush(string key, string value)
        => _retryPolicy.Execute(() => GetDatabase().ListRightPush(key, value));

    public virtual async Task<long> ListRightPushManyAsync(string key, IEnumerable<string> values,
        CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            var redisValues = values.Select(v => (RedisValue)v).ToArray();
            return await db.ListRightPushAsync(key, redisValues).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual long ListRightPushMany(string key, IEnumerable<string> values)
        => _retryPolicy.Execute(() =>
        {
            var redisValues = values.Select(v => (RedisValue)v).ToArray();
            return GetDatabase().ListRightPush(key, redisValues);
        });

    public virtual async Task<string?> ListLeftPopAsync(string key, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            var result = await db.ListLeftPopAsync(key).ConfigureAwait(false);
            return result.HasValue ? (string?)result : null;
        }, ct).ConfigureAwait(false);

    public virtual string? ListLeftPop(string key)
        => _retryPolicy.Execute(() =>
        {
            var result = GetDatabase().ListLeftPop(key);
            return result.HasValue ? (string?)result : null;
        });

    public virtual async Task<string?> ListRightPopAsync(string key, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            var result = await db.ListRightPopAsync(key).ConfigureAwait(false);
            return result.HasValue ? (string?)result : null;
        }, ct).ConfigureAwait(false);

    public virtual string? ListRightPop(string key)
        => _retryPolicy.Execute(() =>
        {
            var result = GetDatabase().ListRightPop(key);
            return result.HasValue ? (string?)result : null;
        });

    public virtual async Task<string?> ListLeftPopRightPushAsync(string source, string destination,
        CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            var result = await db.ListMoveAsync(source, destination, ListSide.Left, ListSide.Right)
                .ConfigureAwait(false);
            return result.HasValue ? (string?)result : null;
        }, ct).ConfigureAwait(false);

    public virtual string? ListLeftPopRightPush(string source, string destination)
        => _retryPolicy.Execute(() =>
        {
            var result = GetDatabase().ListMove(source, destination, ListSide.Left, ListSide.Right);
            return result.HasValue ? (string?)result : null;
        });

    public virtual async Task<IEnumerable<string>> ListRangeAsync(string key, long start = 0, long stop = -1,
        CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            var results = await db.ListRangeAsync(key, start, stop).ConfigureAwait(false);
            return results.Select(r => (string)r!).ToList();
        }, ct).ConfigureAwait(false);

    public virtual IEnumerable<string> ListRange(string key, long start = 0, long stop = -1)
        => _retryPolicy.Execute(() =>
            GetDatabase().ListRange(key, start, stop).Select(r => (string)r!));

    public virtual async Task<long> ListLengthAsync(string key, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            return await db.ListLengthAsync(key).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual long ListLength(string key)
        => _retryPolicy.Execute(() => GetDatabase().ListLength(key));

    public virtual async Task<string?> ListGetByIndexAsync(string key, long index, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            var result = await db.ListGetByIndexAsync(key, index).ConfigureAwait(false);
            return result.HasValue ? (string?)result : null;
        }, ct).ConfigureAwait(false);

    public virtual string? ListGetByIndex(string key, long index)
        => _retryPolicy.Execute(() =>
        {
            var result = GetDatabase().ListGetByIndex(key, index);
            return result.HasValue ? (string?)result : null;
        });

    public virtual async Task ListSetByIndexAsync(string key, long index, string value, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            await db.ListSetByIndexAsync(key, index, value).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual void ListSetByIndex(string key, long index, string value)
        => _retryPolicy.Execute(() => GetDatabase().ListSetByIndex(key, index, value));

    public virtual async Task<long> ListRemoveAsync(string key, string value, long count = 0,
        CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            return await db.ListRemoveAsync(key, value, count).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual long ListRemove(string key, string value, long count = 0)
        => _retryPolicy.Execute(() => GetDatabase().ListRemove(key, value, count));

    public virtual async Task ListTrimAsync(string key, long start, long stop, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            await db.ListTrimAsync(key, start, stop).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual void ListTrim(string key, long start, long stop)
        => _retryPolicy.Execute(() => GetDatabase().ListTrim(key, start, stop));

    // =========================================================================
    // Set Operations
    // =========================================================================

    public virtual async Task<bool> SetAddAsync(string key, string member, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            return await db.SetAddAsync(key, member).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual bool SetAdd(string key, string member)
        => _retryPolicy.Execute(() => GetDatabase().SetAdd(key, member));

    public virtual async Task<long> SetAddManyAsync(string key, IEnumerable<string> members,
        CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            var redisValues = members.Select(m => (RedisValue)m).ToArray();
            return await db.SetAddAsync(key, redisValues).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual long SetAddMany(string key, IEnumerable<string> members)
        => _retryPolicy.Execute(() =>
        {
            var redisValues = members.Select(m => (RedisValue)m).ToArray();
            return GetDatabase().SetAdd(key, redisValues);
        });

    public virtual async Task<bool> SetRemoveAsync(string key, string member, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            return await db.SetRemoveAsync(key, member).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual bool SetRemove(string key, string member)
        => _retryPolicy.Execute(() => GetDatabase().SetRemove(key, member));

    public virtual async Task<long> SetRemoveManyAsync(string key, IEnumerable<string> members,
        CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            var redisValues = members.Select(m => (RedisValue)m).ToArray();
            return await db.SetRemoveAsync(key, redisValues).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual long SetRemoveMany(string key, IEnumerable<string> members)
        => _retryPolicy.Execute(() =>
        {
            var redisValues = members.Select(m => (RedisValue)m).ToArray();
            return GetDatabase().SetRemove(key, redisValues);
        });

    public virtual async Task<bool> SetContainsAsync(string key, string member, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            return await db.SetContainsAsync(key, member).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual bool SetContains(string key, string member)
        => _retryPolicy.Execute(() => GetDatabase().SetContains(key, member));

    public virtual async Task<IEnumerable<string>> SetMembersAsync(string key, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            var results = await db.SetMembersAsync(key).ConfigureAwait(false);
            return results.Select(r => (string)r!).ToList();
        }, ct).ConfigureAwait(false);

    public virtual IEnumerable<string> SetMembers(string key)
        => _retryPolicy.Execute(() => GetDatabase().SetMembers(key).Select(r => (string)r!));

    public virtual async Task<long> SetLengthAsync(string key, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            return await db.SetLengthAsync(key).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual long SetLength(string key)
        => _retryPolicy.Execute(() => GetDatabase().SetLength(key));

    public virtual async Task<string?> SetRandomMemberAsync(string key, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            var result = await db.SetRandomMemberAsync(key).ConfigureAwait(false);
            return result.HasValue ? (string?)result : null;
        }, ct).ConfigureAwait(false);

    public virtual string? SetRandomMember(string key)
        => _retryPolicy.Execute(() =>
        {
            var result = GetDatabase().SetRandomMember(key);
            return result.HasValue ? (string?)result : null;
        });

    public virtual async Task<IEnumerable<string>> SetRandomMembersAsync(string key, long count,
        CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            var results = await db.SetRandomMembersAsync(key, count).ConfigureAwait(false);
            return results.Select(r => (string)r!);
        }, ct).ConfigureAwait(false);

    public virtual IEnumerable<string> SetRandomMembers(string key, long count)
        => _retryPolicy.Execute(() => GetDatabase().SetRandomMembers(key, count).Select(r => (string)r!));

    public virtual async Task<IEnumerable<string>> SetCombineAsync(SetOperation operation, string first, string second,
        CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            var results = await db.SetCombineAsync(operation, first, second).ConfigureAwait(false);
            return results.Select(r => (string)r!).ToList();
        }, ct).ConfigureAwait(false);

    public virtual IEnumerable<string> SetCombine(SetOperation operation, string first, string second)
        => _retryPolicy.Execute(() => GetDatabase().SetCombine(operation, first, second).Select(r => (string)r!));

    public virtual async Task<long> SetCombineAndStoreAsync(SetOperation operation, string destination, string first,
        string second, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            return await db.SetCombineAndStoreAsync(operation, destination, first, second).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual long SetCombineAndStore(SetOperation operation, string destination, string first, string second)
        => _retryPolicy.Execute(() => GetDatabase().SetCombineAndStore(operation, destination, first, second));

    public virtual async Task<string?> SetPopAsync(string key, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            var result = await db.SetPopAsync(key).ConfigureAwait(false);
            return result.HasValue ? (string?)result : null;
        }, ct).ConfigureAwait(false);

    public virtual string? SetPop(string key)
        => _retryPolicy.Execute(() =>
        {
            var result = GetDatabase().SetPop(key);
            return result.HasValue ? (string?)result : null;
        });

    public virtual async Task<bool> SetMoveAsync(string source, string destination, string member,
        CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            return await db.SetMoveAsync(source, destination, member).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual bool SetMove(string source, string destination, string member)
        => _retryPolicy.Execute(() => GetDatabase().SetMove(source, destination, member));

    // =========================================================================
    // Sorted Set Operations
    // =========================================================================

    public virtual async Task<bool> SortedSetAddAsync(string key, string member, double score,
        CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            return await db.SortedSetAddAsync(key, member, score).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual bool SortedSetAdd(string key, string member, double score)
        => _retryPolicy.Execute(() => GetDatabase().SortedSetAdd(key, member, score));

    public virtual async Task<long> SortedSetAddManyAsync(string key,
        IEnumerable<(string member, double score)> entries, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            var sortedEntries = entries.Select(e => new SortedSetEntry(e.member, e.score)).ToArray();
            return await db.SortedSetAddAsync(key, sortedEntries).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual long SortedSetAddMany(string key, IEnumerable<(string member, double score)> entries)
        => _retryPolicy.Execute(() =>
        {
            var sortedEntries = entries.Select(e => new SortedSetEntry(e.member, e.score)).ToArray();
            return GetDatabase().SortedSetAdd(key, sortedEntries);
        });

    public virtual async Task<bool> SortedSetRemoveAsync(string key, string member, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            return await db.SortedSetRemoveAsync(key, member).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual bool SortedSetRemove(string key, string member)
        => _retryPolicy.Execute(() => GetDatabase().SortedSetRemove(key, member));

    public virtual async Task<long> SortedSetRemoveManyAsync(string key, IEnumerable<string> members,
        CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            var redisValues = members.Select(m => (RedisValue)m).ToArray();
            return await db.SortedSetRemoveAsync(key, redisValues).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual long SortedSetRemoveMany(string key, IEnumerable<string> members)
        => _retryPolicy.Execute(() =>
        {
            var redisValues = members.Select(m => (RedisValue)m).ToArray();
            return GetDatabase().SortedSetRemove(key, redisValues);
        });

    public virtual async Task<double?> SortedSetScoreAsync(string key, string member, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            return await db.SortedSetScoreAsync(key, member).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual double? SortedSetScore(string key, string member)
        => _retryPolicy.Execute(() => GetDatabase().SortedSetScore(key, member));

    public virtual async Task<double> SortedSetIncrementAsync(string key, string member, double value,
        CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            return await db.SortedSetIncrementAsync(key, member, value).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual double SortedSetIncrement(string key, string member, double value)
        => _retryPolicy.Execute(() => GetDatabase().SortedSetIncrement(key, member, value));

    public virtual async Task<long?> SortedSetRankAsync(string key, string member, Order order = Order.Ascending,
        CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            return await db.SortedSetRankAsync(key, member, order).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual long? SortedSetRank(string key, string member, Order order = Order.Ascending)
        => _retryPolicy.Execute(() => GetDatabase().SortedSetRank(key, member, order));

    public virtual async Task<IEnumerable<string>> SortedSetRangeByRankAsync(string key, long start = 0, long stop = -1,
        Order order = Order.Ascending, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            var results = await db.SortedSetRangeByRankAsync(key, start, stop, order).ConfigureAwait(false);
            return results.Select(r => (string)r!).ToList();
        }, ct).ConfigureAwait(false);

    public virtual IEnumerable<string> SortedSetRangeByRank(string key, long start = 0, long stop = -1,
        Order order = Order.Ascending)
        => _retryPolicy.Execute(() =>
            GetDatabase().SortedSetRangeByRank(key, start, stop, order).Select(r => (string)r!));

    public virtual async Task<IEnumerable<string>> SortedSetRangeByScoreAsync(string key,
        double start = double.NegativeInfinity, double stop = double.PositiveInfinity, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            var results = await db.SortedSetRangeByScoreAsync(key, start, stop).ConfigureAwait(false);
            return results.Select(r => (string)r!).ToList();
        }, ct).ConfigureAwait(false);

    public virtual IEnumerable<string> SortedSetRangeByScore(string key, double start = double.NegativeInfinity,
        double stop = double.PositiveInfinity)
        => _retryPolicy.Execute(() =>
            GetDatabase().SortedSetRangeByScore(key, start, stop).Select(r => (string)r!));

    public virtual async Task<long> SortedSetLengthAsync(string key, double min = double.NegativeInfinity,
        double max = double.PositiveInfinity, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            return await db.SortedSetLengthAsync(key, min, max).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual long SortedSetLength(string key, double min = double.NegativeInfinity,
        double max = double.PositiveInfinity)
        => _retryPolicy.Execute(() => GetDatabase().SortedSetLength(key, min, max));

    public virtual async Task<long> SortedSetRemoveRangeByRankAsync(string key, long start, long stop,
        CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            return await db.SortedSetRemoveRangeByRankAsync(key, start, stop).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual long SortedSetRemoveRangeByRank(string key, long start, long stop)
        => _retryPolicy.Execute(() => GetDatabase().SortedSetRemoveRangeByRank(key, start, stop));

    public virtual async Task<long> SortedSetRemoveRangeByScoreAsync(string key, double start, double stop,
        CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            return await db.SortedSetRemoveRangeByScoreAsync(key, start, stop).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual long SortedSetRemoveRangeByScore(string key, double start, double stop)
        => _retryPolicy.Execute(() => GetDatabase().SortedSetRemoveRangeByScore(key, start, stop));

    // =========================================================================
    // HyperLogLog Operations
    // =========================================================================

    public virtual async Task<bool> HyperLogLogAddAsync(string key, string value, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            return await db.HyperLogLogAddAsync(key, value).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual bool HyperLogLogAdd(string key, string value)
        => _retryPolicy.Execute(() => GetDatabase().HyperLogLogAdd(key, value));

    public virtual async Task<bool> HyperLogLogAddManyAsync(string key, IEnumerable<string> values,
        CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            var redisValues = values.Select(v => (RedisValue)v).ToArray();
            return await db.HyperLogLogAddAsync(key, redisValues).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual bool HyperLogLogAddMany(string key, IEnumerable<string> values)
        => _retryPolicy.Execute(() =>
        {
            var redisValues = values.Select(v => (RedisValue)v).ToArray();
            return GetDatabase().HyperLogLogAdd(key, redisValues);
        });

    public virtual async Task<long> HyperLogLogLengthAsync(string key, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            return await db.HyperLogLogLengthAsync(key).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual long HyperLogLogLength(string key)
        => _retryPolicy.Execute(() => GetDatabase().HyperLogLogLength(key));

    public virtual async Task<long> HyperLogLogLengthManyAsync(IEnumerable<string> keys, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            var redisKeys = keys.Select(k => (RedisKey)k).ToArray();
            return await db.HyperLogLogLengthAsync(redisKeys).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual long HyperLogLogLengthMany(IEnumerable<string> keys)
        => _retryPolicy.Execute(() =>
        {
            var redisKeys = keys.Select(k => (RedisKey)k).ToArray();
            return GetDatabase().HyperLogLogLength(redisKeys);
        });

    public virtual async Task HyperLogLogMergeAsync(string destination, string first, string second,
        CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            await db.HyperLogLogMergeAsync(destination, first, second).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual void HyperLogLogMerge(string destination, string first, string second)
        => _retryPolicy.Execute(() => GetDatabase().HyperLogLogMerge(destination, first, second));

    public virtual async Task HyperLogLogMergeManyAsync(string destination, IEnumerable<string> sourceKeys,
        CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            var redisKeys = sourceKeys.Select(k => (RedisKey)k).ToArray();
            await db.HyperLogLogMergeAsync(destination, redisKeys).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual void HyperLogLogMergeMany(string destination, IEnumerable<string> sourceKeys)
        => _retryPolicy.Execute(() =>
        {
            var redisKeys = sourceKeys.Select(k => (RedisKey)k).ToArray();
            GetDatabase().HyperLogLogMerge(destination, redisKeys);
        });

    // =========================================================================
    // Bitmap Operations
    // =========================================================================

    public virtual async Task<bool> BitmapSetAsync(string key, long offset, bool bit, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            return await db.StringSetBitAsync(key, offset, bit).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual bool BitmapSet(string key, long offset, bool bit)
        => _retryPolicy.Execute(() => GetDatabase().StringSetBit(key, offset, bit));

    public virtual async Task<bool> BitmapGetAsync(string key, long offset, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            return await db.StringGetBitAsync(key, offset).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual bool BitmapGet(string key, long offset)
        => _retryPolicy.Execute(() => GetDatabase().StringGetBit(key, offset));

    public virtual async Task<long> BitmapCountAsync(string key, long start = 0, long end = -1,
        CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            return await db.StringBitCountAsync(key, start, end).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual long BitmapCount(string key, long start = 0, long end = -1)
        => _retryPolicy.Execute(() => GetDatabase().StringBitCount(key, start, end));

    public virtual async Task<long> BitmapOperationAsync(Bitwise operation, string destination, string first,
        string second, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            return await db.StringBitOperationAsync(operation, destination, first, second).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual long BitmapOperation(Bitwise operation, string destination, string first, string second)
        => _retryPolicy.Execute(() => GetDatabase().StringBitOperation(operation, destination, first, second));

    public virtual async Task<long> BitmapPositionAsync(string key, bool bit, long start = 0, long end = -1,
        CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            return await db.StringBitPositionAsync(key, bit, start, end).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual long BitmapPosition(string key, bool bit, long start = 0, long end = -1)
        => _retryPolicy.Execute(() => GetDatabase().StringBitPosition(key, bit, start, end));

    // =========================================================================
    // Geo Operations
    // =========================================================================

    public virtual async Task<bool> GeoAddAsync(string key, double longitude, double latitude, string member,
        CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            return await db.GeoAddAsync(key, longitude, latitude, member).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual bool GeoAdd(string key, double longitude, double latitude, string member)
        => _retryPolicy.Execute(() => GetDatabase().GeoAdd(key, longitude, latitude, member));

    public virtual async Task<long> GeoAddManyAsync(string key,
        IEnumerable<(double longitude, double latitude, string member)> entries, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            var geoEntries = entries.Select(e => new GeoEntry(e.longitude, e.latitude, e.member)).ToArray();
            return await db.GeoAddAsync(key, geoEntries).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual long GeoAddMany(string key, IEnumerable<(double longitude, double latitude, string member)> entries)
        => _retryPolicy.Execute(() =>
        {
            var geoEntries = entries.Select(e => new GeoEntry(e.longitude, e.latitude, e.member)).ToArray();
            return GetDatabase().GeoAdd(key, geoEntries);
        });

    public virtual async Task<double?> GeoDistanceAsync(string key, string member1, string member2,
        GeoUnit unit = GeoUnit.Meters, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            return await db.GeoDistanceAsync(key, member1, member2, unit).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual double? GeoDistance(string key, string member1, string member2, GeoUnit unit = GeoUnit.Meters)
        => _retryPolicy.Execute(() => GetDatabase().GeoDistance(key, member1, member2, unit));

    public virtual async Task<string?[]> GeoHashAsync(string key, IEnumerable<string> members,
        CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            var redisMembers = members.Select(m => (RedisValue)m).ToArray();
            return await db.GeoHashAsync(key, redisMembers).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual string?[] GeoHash(string key, IEnumerable<string> members)
        => _retryPolicy.Execute(() =>
        {
            var redisMembers = members.Select(m => (RedisValue)m).ToArray();
            return GetDatabase().GeoHash(key, redisMembers);
        });

    public virtual async Task<IEnumerable<(string member, double? longitude, double? latitude)>> GeoPositionAsync(
        string key, IEnumerable<string> members, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            var memberList = members.ToList();
            var redisMembers = memberList.Select(m => (RedisValue)m).ToArray();
            var results = await db.GeoPositionAsync(key, redisMembers).ConfigureAwait(false);
            return results.Select((r, i) => (member: memberList[i], position: r))
                .Where(x => x.position.HasValue)
                .Select(x => (x.member, (double?)x.position!.Value.Longitude, (double?)x.position!.Value.Latitude));
        }, ct).ConfigureAwait(false);

    public virtual IEnumerable<(string member, double? longitude, double? latitude)> GeoPosition(string key,
        IEnumerable<string> members)
        => _retryPolicy.Execute(() =>
        {
            var memberList = members.ToList();
            var redisMembers = memberList.Select(m => (RedisValue)m).ToArray();
            var results = GetDatabase().GeoPosition(key, redisMembers);
            return results.Select((r, i) => (member: memberList[i], position: r))
                .Where(x => x.position.HasValue)
                .Select(x => (x.member, (double?)x.position!.Value.Longitude, (double?)x.position!.Value.Latitude));
        });

    public virtual async Task<IEnumerable<string>> GeoRadiusAsync(string key, string member, double radius,
        GeoUnit unit = GeoUnit.Meters, int count = -1, Order order = Order.Ascending, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            var results = await db.GeoRadiusAsync(key, member, radius, unit, count, order).ConfigureAwait(false);
            return results.Select(r => r.Member.ToString());
        }, ct).ConfigureAwait(false);

    public virtual IEnumerable<string> GeoRadius(string key, string member, double radius,
        GeoUnit unit = GeoUnit.Meters, int count = -1, Order order = Order.Ascending)
        => _retryPolicy.Execute(() =>
        {
            var results = GetDatabase().GeoRadius(key, member, radius, unit, count, order);
            return results.Select(r => r.Member.ToString());
        });

    public virtual async Task<bool> GeoRemoveAsync(string key, string member, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            return await db.GeoRemoveAsync(key, member).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual bool GeoRemove(string key, string member)
        => _retryPolicy.Execute(() => GetDatabase().GeoRemove(key, member));

    // =========================================================================
    // Pub/Sub
    // =========================================================================

    public virtual async Task<long> PublishAsync(string channel, string message, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var sub = _connectionProvider.GetSubscriber();
            return await sub.PublishAsync(RedisChannel.Literal(channel), message).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual long Publish(string channel, string message)
        => _retryPolicy.Execute(() =>
            _connectionProvider.GetSubscriber().Publish(RedisChannel.Literal(channel), message));

    public virtual async Task SubscribeAsync(string channel, Action<string, string> handler,
        CancellationToken ct = default)
    {
        var sub = _connectionProvider.GetSubscriber();
        await sub.SubscribeAsync(RedisChannel.Literal(channel), (ch, val) =>
        {
            try
            {
                handler(ch.ToString(), val.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis 订阅回调异常，Channel: {Channel}", channel);
            }
        }).ConfigureAwait(false);
    }

    public virtual void Subscribe(string channel, Action<string, string> handler)
    {
        var sub = _connectionProvider.GetSubscriber();
        sub.Subscribe(RedisChannel.Literal(channel), (ch, val) =>
        {
            try
            {
                handler(ch.ToString(), val.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis 订阅回调异常，Channel: {Channel}", channel);
            }
        });
    }

    public virtual async Task UnsubscribeAsync(string channel, CancellationToken ct = default)
    {
        var sub = _connectionProvider.GetSubscriber();
        await sub.UnsubscribeAsync(RedisChannel.Literal(channel)).ConfigureAwait(false);
    }

    public virtual void Unsubscribe(string channel)
    {
        var sub = _connectionProvider.GetSubscriber();
        sub.Unsubscribe(RedisChannel.Literal(channel));
    }

    // =========================================================================
    // Distributed Lock
    // =========================================================================

    public virtual async Task<bool> AcquireLockAsync(string lockKey, string lockValue, TimeSpan expiry,
        CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            return await db.LockTakeAsync(lockKey, lockValue, expiry).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual bool AcquireLock(string lockKey, string lockValue, TimeSpan expiry)
        => _retryPolicy.Execute(() => GetDatabase().LockTake(lockKey, lockValue, expiry));

    public virtual async Task<bool> ReleaseLockAsync(string lockKey, string lockValue, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            return await db.LockReleaseAsync(lockKey, lockValue).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual bool ReleaseLock(string lockKey, string lockValue)
        => _retryPolicy.Execute(() => GetDatabase().LockRelease(lockKey, lockValue));

    public virtual async Task<bool> ExtendLockAsync(string lockKey, string lockValue, TimeSpan additionalExpiry,
        CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            return await db.LockExtendAsync(lockKey, lockValue, additionalExpiry).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual bool ExtendLock(string lockKey, string lockValue, TimeSpan additionalExpiry)
        => _retryPolicy.Execute(() => GetDatabase().LockExtend(lockKey, lockValue, additionalExpiry));

    // =========================================================================
    // Scripting & Pipeline
    // =========================================================================

    public virtual async Task<RedisResult> ScriptEvaluateAsync(string script, RedisKey[]? keys = null,
        RedisValue[]? values = null, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            return await db.ScriptEvaluateAsync(script, keys, values).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual RedisResult ScriptEvaluate(string script, RedisKey[]? keys = null, RedisValue[]? values = null)
        => _retryPolicy.Execute(() => GetDatabase().ScriptEvaluate(script, keys, values));

    public virtual async Task<RedisResult> ScriptEvaluateAsync(LoadedLuaScript loadedScript, object? parameters = null,
        CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            return await db.ScriptEvaluateAsync(loadedScript, parameters).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual RedisResult ScriptEvaluate(LoadedLuaScript loadedScript, object? parameters = null)
        => _retryPolicy.Execute(() => GetDatabase().ScriptEvaluate(loadedScript, parameters));

    public virtual IBatch CreateBatch()
        => GetDatabase().CreateBatch();

    public virtual async Task ExecuteBatchAsync(IBatch batch, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            batch.Execute();
            await Task.CompletedTask;
        }, ct).ConfigureAwait(false);

    public virtual void ExecuteBatch(IBatch batch)
        => _retryPolicy.Execute(batch.Execute);

    public virtual ITransaction CreateTransaction()
        => GetDatabase().CreateTransaction();

    // =========================================================================
    // Utility
    // =========================================================================

    public virtual async Task FlushDatabaseAsync(int db = -1, CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var server = _connectionProvider.GetConnection().GetServers().FirstOrDefault()
                         ?? throw new InvalidOperationException("没有可用的 Redis 服务器");
            await server.FlushDatabaseAsync(db).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual void FlushDatabase(int db = -1)
        => _retryPolicy.Execute(() =>
        {
            var server = _connectionProvider.GetConnection().GetServers().FirstOrDefault()
                         ?? throw new InvalidOperationException("没有可用的 Redis 服务器");
            server.FlushDatabase(db);
        });

    public virtual async Task<TimeSpan> PingAsync(CancellationToken ct = default)
        => await _retryPolicy.ExecuteAsync(async () =>
        {
            var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
            return await db.PingAsync().ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

    public virtual TimeSpan Ping()
        => _retryPolicy.Execute(() => GetDatabase().Ping());

    /// <summary>
    /// 获取当前使用的数据库实例。
    /// </summary>
    protected virtual async Task<IDatabase> GetDatabaseAsync(CancellationToken ct = default)
    {
        return await _connectionProvider.GetDatabaseAsync().WaitAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// 获取当前使用的数据库实例（同步）。
    /// </summary>
    protected virtual IDatabase GetDatabase()
    {
        return _connectionProvider.GetConnection().GetDatabase();
    }
}
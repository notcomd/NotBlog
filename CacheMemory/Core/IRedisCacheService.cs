using StackExchange.Redis;

namespace CacheMemory.Core;

/// <summary>
/// Redis 缓存服务的综合接口，提供对 Redis 所有数据类型的完整操作支持。
/// 所有异步方法均支持 <see cref="CancellationToken"/> 取消，并提供对应的同步重载。
/// </summary>
/// <remarks>
/// 此接口中的方法均内置重试策略（通过 <see cref="IRedisRetryPolicy"/>），
/// 对调用方透明。扩展方法使用 virtual 默认实现，允许子类覆写特定逻辑。
/// </remarks>
public interface IRedisCacheService
{
    #region Key Management

    Task<bool> KeyExistsAsync(string key, CancellationToken ct = default);
    bool KeyExists(string key);
    Task<bool> KeyDeleteAsync(string key, CancellationToken ct = default);
    bool KeyDelete(string key);
    Task<long> KeyDeleteManyAsync(IEnumerable<string> keys, CancellationToken ct = default);
    long KeyDeleteMany(IEnumerable<string> keys);
    Task<bool> KeyExpireAsync(string key, TimeSpan expiry, CancellationToken ct = default);
    bool KeyExpire(string key, TimeSpan expiry);
    Task<bool> KeyPersistAsync(string key, CancellationToken ct = default);
    bool KeyPersist(string key);
    Task<TimeSpan?> KeyTtlAsync(string key, CancellationToken ct = default);
    TimeSpan? KeyTtl(string key);
    Task<RedisType> KeyTypeAsync(string key, CancellationToken ct = default);
    RedisType KeyType(string key);
    Task<bool> KeyRenameAsync(string key, string newKey, CancellationToken ct = default);
    bool KeyRename(string key, string newKey);
    Task<IEnumerable<string>> KeysByPatternAsync(string pattern, int pageSize = 1000, CancellationToken ct = default);
    IEnumerable<string> KeysByPattern(string pattern, int pageSize = 1000);

    #endregion

    #region String Operations

    Task<string?> StringGetAsync(string key, CancellationToken ct = default);
    string? StringGet(string key);
    Task<bool> StringSetAsync(string key, string value, TimeSpan? expiry = null, CancellationToken ct = default);
    bool StringSet(string key, string value, TimeSpan? expiry = null);

    Task<bool> StringSetIfNotExistsAsync(string key, string value, TimeSpan? expiry = null,
        CancellationToken ct = default);

    bool StringSetIfNotExists(string key, string value, TimeSpan? expiry = null);
    Task<long> StringIncrementAsync(string key, long value = 1, CancellationToken ct = default);
    long StringIncrement(string key, long value = 1);
    Task<double> StringIncrementFloatAsync(string key, double value, CancellationToken ct = default);
    double StringIncrementFloat(string key, double value);
    Task<long> StringDecrementAsync(string key, long value = 1, CancellationToken ct = default);
    long StringDecrement(string key, long value = 1);
    Task<long> StringAppendAsync(string key, string value, CancellationToken ct = default);
    long StringAppend(string key, string value);
    Task<long> StringLengthAsync(string key, CancellationToken ct = default);
    long StringLength(string key);
    Task<string?[]> StringGetManyAsync(IEnumerable<string> keys, CancellationToken ct = default);
    string?[] StringGetMany(IEnumerable<string> keys);

    Task<bool> StringSetManyAsync(IDictionary<string, string> entries, TimeSpan? expiry = null,
        CancellationToken ct = default);

    bool StringSetMany(IDictionary<string, string> entries, TimeSpan? expiry = null);

    #endregion

    #region Hash Operations

    Task<bool> HashSetAsync(string key, string field, string value, CancellationToken ct = default);
    bool HashSet(string key, string field, string value);
    Task<string?> HashGetAsync(string key, string field, CancellationToken ct = default);
    string? HashGet(string key, string field);
    Task<bool> HashDeleteAsync(string key, string field, CancellationToken ct = default);
    bool HashDelete(string key, string field);
    Task<long> HashDeleteManyAsync(string key, IEnumerable<string> fields, CancellationToken ct = default);
    long HashDeleteMany(string key, IEnumerable<string> fields);
    Task<bool> HashExistsAsync(string key, string field, CancellationToken ct = default);
    bool HashExists(string key, string field);
    Task<Dictionary<string, string>> HashGetAllAsync(string key, CancellationToken ct = default);
    Dictionary<string, string> HashGetAll(string key);
    Task<IEnumerable<string>> HashKeysAsync(string key, CancellationToken ct = default);
    IEnumerable<string> HashKeys(string key);
    Task<IEnumerable<string>> HashValuesAsync(string key, CancellationToken ct = default);
    IEnumerable<string> HashValues(string key);
    Task<long> HashLengthAsync(string key, CancellationToken ct = default);
    long HashLength(string key);
    Task<long> HashIncrementAsync(string key, string field, long value = 1, CancellationToken ct = default);
    long HashIncrement(string key, string field, long value = 1);
    Task<double> HashIncrementFloatAsync(string key, string field, double value, CancellationToken ct = default);
    double HashIncrementFloat(string key, string field, double value);
    Task<IEnumerable<string?>> HashGetManyAsync(string key, IEnumerable<string> fields, CancellationToken ct = default);
    IEnumerable<string?> HashGetMany(string key, IEnumerable<string> fields);
    Task HashSetManyAsync(string key, IDictionary<string, string> entries, CancellationToken ct = default);
    void HashSetMany(string key, IDictionary<string, string> entries);

    #endregion

    #region List Operations

    Task<long> ListLeftPushAsync(string key, string value, CancellationToken ct = default);
    long ListLeftPush(string key, string value);
    Task<long> ListLeftPushManyAsync(string key, IEnumerable<string> values, CancellationToken ct = default);
    long ListLeftPushMany(string key, IEnumerable<string> values);
    Task<long> ListRightPushAsync(string key, string value, CancellationToken ct = default);
    long ListRightPush(string key, string value);
    Task<long> ListRightPushManyAsync(string key, IEnumerable<string> values, CancellationToken ct = default);
    long ListRightPushMany(string key, IEnumerable<string> values);
    Task<string?> ListLeftPopAsync(string key, CancellationToken ct = default);
    string? ListLeftPop(string key);
    Task<string?> ListRightPopAsync(string key, CancellationToken ct = default);
    string? ListRightPop(string key);
    Task<string?> ListLeftPopRightPushAsync(string source, string destination, CancellationToken ct = default);
    string? ListLeftPopRightPush(string source, string destination);

    Task<IEnumerable<string>> ListRangeAsync(string key, long start = 0, long stop = -1,
        CancellationToken ct = default);

    IEnumerable<string> ListRange(string key, long start = 0, long stop = -1);
    Task<long> ListLengthAsync(string key, CancellationToken ct = default);
    long ListLength(string key);
    Task<string?> ListGetByIndexAsync(string key, long index, CancellationToken ct = default);
    string? ListGetByIndex(string key, long index);
    Task ListSetByIndexAsync(string key, long index, string value, CancellationToken ct = default);
    void ListSetByIndex(string key, long index, string value);
    Task<long> ListRemoveAsync(string key, string value, long count = 0, CancellationToken ct = default);
    long ListRemove(string key, string value, long count = 0);
    Task ListTrimAsync(string key, long start, long stop, CancellationToken ct = default);
    void ListTrim(string key, long start, long stop);

    #endregion

    #region Set Operations

    Task<bool> SetAddAsync(string key, string member, CancellationToken ct = default);
    bool SetAdd(string key, string member);
    Task<long> SetAddManyAsync(string key, IEnumerable<string> members, CancellationToken ct = default);
    long SetAddMany(string key, IEnumerable<string> members);
    Task<bool> SetRemoveAsync(string key, string member, CancellationToken ct = default);
    bool SetRemove(string key, string member);
    Task<long> SetRemoveManyAsync(string key, IEnumerable<string> members, CancellationToken ct = default);
    long SetRemoveMany(string key, IEnumerable<string> members);
    Task<bool> SetContainsAsync(string key, string member, CancellationToken ct = default);
    bool SetContains(string key, string member);
    Task<IEnumerable<string>> SetMembersAsync(string key, CancellationToken ct = default);
    IEnumerable<string> SetMembers(string key);
    Task<long> SetLengthAsync(string key, CancellationToken ct = default);
    long SetLength(string key);
    Task<string?> SetRandomMemberAsync(string key, CancellationToken ct = default);
    string? SetRandomMember(string key);
    Task<IEnumerable<string>> SetRandomMembersAsync(string key, long count, CancellationToken ct = default);
    IEnumerable<string> SetRandomMembers(string key, long count);

    Task<IEnumerable<string>> SetCombineAsync(SetOperation operation, string first, string second,
        CancellationToken ct = default);

    IEnumerable<string> SetCombine(SetOperation operation, string first, string second);

    Task<long> SetCombineAndStoreAsync(SetOperation operation, string destination, string first, string second,
        CancellationToken ct = default);

    long SetCombineAndStore(SetOperation operation, string destination, string first, string second);
    Task<string?> SetPopAsync(string key, CancellationToken ct = default);
    string? SetPop(string key);
    Task<bool> SetMoveAsync(string source, string destination, string member, CancellationToken ct = default);
    bool SetMove(string source, string destination, string member);

    #endregion

    #region Sorted Set Operations

    Task<bool> SortedSetAddAsync(string key, string member, double score, CancellationToken ct = default);
    bool SortedSetAdd(string key, string member, double score);

    Task<long> SortedSetAddManyAsync(string key, IEnumerable<(string member, double score)> entries,
        CancellationToken ct = default);

    long SortedSetAddMany(string key, IEnumerable<(string member, double score)> entries);
    Task<bool> SortedSetRemoveAsync(string key, string member, CancellationToken ct = default);
    bool SortedSetRemove(string key, string member);
    Task<long> SortedSetRemoveManyAsync(string key, IEnumerable<string> members, CancellationToken ct = default);
    long SortedSetRemoveMany(string key, IEnumerable<string> members);
    Task<double?> SortedSetScoreAsync(string key, string member, CancellationToken ct = default);
    double? SortedSetScore(string key, string member);
    Task<double> SortedSetIncrementAsync(string key, string member, double value, CancellationToken ct = default);
    double SortedSetIncrement(string key, string member, double value);

    Task<long?> SortedSetRankAsync(string key, string member, Order order = Order.Ascending,
        CancellationToken ct = default);

    long? SortedSetRank(string key, string member, Order order = Order.Ascending);

    Task<IEnumerable<string>> SortedSetRangeByRankAsync(string key, long start = 0, long stop = -1,
        Order order = Order.Ascending, CancellationToken ct = default);

    IEnumerable<string> SortedSetRangeByRank(string key, long start = 0, long stop = -1, Order order = Order.Ascending);

    Task<IEnumerable<string>> SortedSetRangeByScoreAsync(string key, double start = double.NegativeInfinity,
        double stop = double.PositiveInfinity, CancellationToken ct = default);

    IEnumerable<string> SortedSetRangeByScore(string key, double start = double.NegativeInfinity,
        double stop = double.PositiveInfinity);

    Task<long> SortedSetLengthAsync(string key, double min = double.NegativeInfinity,
        double max = double.PositiveInfinity, CancellationToken ct = default);

    long SortedSetLength(string key, double min = double.NegativeInfinity, double max = double.PositiveInfinity);
    Task<long> SortedSetRemoveRangeByRankAsync(string key, long start, long stop, CancellationToken ct = default);
    long SortedSetRemoveRangeByRank(string key, long start, long stop);
    Task<long> SortedSetRemoveRangeByScoreAsync(string key, double start, double stop, CancellationToken ct = default);
    long SortedSetRemoveRangeByScore(string key, double start, double stop);

    #endregion

    #region HyperLogLog Operations

    Task<bool> HyperLogLogAddAsync(string key, string value, CancellationToken ct = default);
    bool HyperLogLogAdd(string key, string value);
    Task<bool> HyperLogLogAddManyAsync(string key, IEnumerable<string> values, CancellationToken ct = default);
    bool HyperLogLogAddMany(string key, IEnumerable<string> values);
    Task<long> HyperLogLogLengthAsync(string key, CancellationToken ct = default);
    long HyperLogLogLength(string key);
    Task<long> HyperLogLogLengthManyAsync(IEnumerable<string> keys, CancellationToken ct = default);
    long HyperLogLogLengthMany(IEnumerable<string> keys);
    Task HyperLogLogMergeAsync(string destination, string first, string second, CancellationToken ct = default);
    void HyperLogLogMerge(string destination, string first, string second);
    Task HyperLogLogMergeManyAsync(string destination, IEnumerable<string> sourceKeys, CancellationToken ct = default);
    void HyperLogLogMergeMany(string destination, IEnumerable<string> sourceKeys);

    #endregion

    #region Bitmap Operations

    Task<bool> BitmapSetAsync(string key, long offset, bool bit, CancellationToken ct = default);
    bool BitmapSet(string key, long offset, bool bit);
    Task<bool> BitmapGetAsync(string key, long offset, CancellationToken ct = default);
    bool BitmapGet(string key, long offset);
    Task<long> BitmapCountAsync(string key, long start = 0, long end = -1, CancellationToken ct = default);
    long BitmapCount(string key, long start = 0, long end = -1);

    Task<long> BitmapOperationAsync(Bitwise operation, string destination, string first, string second,
        CancellationToken ct = default);

    long BitmapOperation(Bitwise operation, string destination, string first, string second);
    Task<long> BitmapPositionAsync(string key, bool bit, long start = 0, long end = -1, CancellationToken ct = default);
    long BitmapPosition(string key, bool bit, long start = 0, long end = -1);

    #endregion

    #region Geo Operations

    Task<bool> GeoAddAsync(string key, double longitude, double latitude, string member,
        CancellationToken ct = default);

    bool GeoAdd(string key, double longitude, double latitude, string member);

    Task<long> GeoAddManyAsync(string key, IEnumerable<(double longitude, double latitude, string member)> entries,
        CancellationToken ct = default);

    long GeoAddMany(string key, IEnumerable<(double longitude, double latitude, string member)> entries);

    Task<double?> GeoDistanceAsync(string key, string member1, string member2, GeoUnit unit = GeoUnit.Meters,
        CancellationToken ct = default);

    double? GeoDistance(string key, string member1, string member2, GeoUnit unit = GeoUnit.Meters);
    Task<string?[]> GeoHashAsync(string key, IEnumerable<string> members, CancellationToken ct = default);
    string?[] GeoHash(string key, IEnumerable<string> members);

    Task<IEnumerable<(string member, double? longitude, double? latitude)>> GeoPositionAsync(string key,
        IEnumerable<string> members, CancellationToken ct = default);

    IEnumerable<(string member, double? longitude, double? latitude)> GeoPosition(string key,
        IEnumerable<string> members);

    Task<IEnumerable<string>> GeoRadiusAsync(string key, string member, double radius, GeoUnit unit = GeoUnit.Meters,
        int count = -1, Order order = Order.Ascending, CancellationToken ct = default);

    IEnumerable<string> GeoRadius(string key, string member, double radius, GeoUnit unit = GeoUnit.Meters,
        int count = -1, Order order = Order.Ascending);

    Task<bool> GeoRemoveAsync(string key, string member, CancellationToken ct = default);
    bool GeoRemove(string key, string member);

    #endregion

    #region Pub/Sub

    Task<long> PublishAsync(string channel, string message, CancellationToken ct = default);
    long Publish(string channel, string message);
    Task SubscribeAsync(string channel, Action<string, string> handler, CancellationToken ct = default);
    void Subscribe(string channel, Action<string, string> handler);
    Task UnsubscribeAsync(string channel, CancellationToken ct = default);
    void Unsubscribe(string channel);

    #endregion

    #region Distributed Lock

    Task<bool> AcquireLockAsync(string lockKey, string lockValue, TimeSpan expiry, CancellationToken ct = default);
    bool AcquireLock(string lockKey, string lockValue, TimeSpan expiry);
    Task<bool> ReleaseLockAsync(string lockKey, string lockValue, CancellationToken ct = default);
    bool ReleaseLock(string lockKey, string lockValue);

    Task<bool> ExtendLockAsync(string lockKey, string lockValue, TimeSpan additionalExpiry,
        CancellationToken ct = default);

    bool ExtendLock(string lockKey, string lockValue, TimeSpan additionalExpiry);

    #endregion

    #region Scripting & Pipeline

    Task<RedisResult> ScriptEvaluateAsync(string script, RedisKey[]? keys = null, RedisValue[]? values = null,
        CancellationToken ct = default);

    RedisResult ScriptEvaluate(string script, RedisKey[]? keys = null, RedisValue[]? values = null);

    Task<RedisResult> ScriptEvaluateAsync(LoadedLuaScript loadedScript, object? parameters = null,
        CancellationToken ct = default);

    RedisResult ScriptEvaluate(LoadedLuaScript loadedScript, object? parameters = null);
    IBatch CreateBatch();
    Task ExecuteBatchAsync(IBatch batch, CancellationToken ct = default);
    void ExecuteBatch(IBatch batch);
    ITransaction CreateTransaction();

    #endregion

    #region Utility

    Task FlushDatabaseAsync(int db = -1, CancellationToken ct = default);
    void FlushDatabase(int db = -1);
    Task<TimeSpan> PingAsync(CancellationToken ct = default);
    TimeSpan Ping();

    #endregion
}
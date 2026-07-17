using CacheMemory.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using StackExchange.Redis;

namespace CacheMemory.Providers;

/// <summary>
/// Redis 连接提供者实现。
/// 管理多个 Redis 实例的连接生命周期，按名称提供对应的 IConnectionMultiplexer。
/// </summary>
public sealed class RedisConnectionProvider : IRedisConnectionProvider
{
    private readonly Dictionary<string, CacheMemoryConnection> _connections = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private readonly ILogger<RedisConnectionProvider> _logger;
    private readonly CacheMemoryOption _options;
    private volatile bool _isDisposed;

    public RedisConnectionProvider(CacheMemoryOption options, ILogger<RedisConnectionProvider>? logger = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? NullLogger<RedisConnectionProvider>.Instance;
    }

    
    public IConnectionMultiplexer GetConnection()
    {
        return GetConnection(null);
    }

   
    public IConnectionMultiplexer GetConnection(string? instanceName)
    {
        EnsureNotDisposed();
        instanceName = NormalizeInstanceName(instanceName);
        var connection = GetOrCreateConnection(instanceName);
        return connection.GetConnection();
    }

    
    public async Task<IDatabase> GetDatabaseAsync(int db = -1)
    {
        var conn = GetConnection();
        return conn.GetDatabase(db);
    }

    
    public async Task<IDatabase> GetDatabaseAsync(string instanceName, int db = -1)
    {
        var conn = GetConnection(instanceName);
        return conn.GetDatabase(db);
    }

    
    public ISubscriber GetSubscriber()
    {
        return GetSubscriber(null);
    }

    
    public ISubscriber GetSubscriber(string? instanceName)
    {
        var conn = GetConnection(instanceName);
        return conn.GetSubscriber();
    }

    
    public IReadOnlyCollection<string> GetInstanceNames()
    {
        return _options.Instances.Keys.ToList().AsReadOnly();
    }

    
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        foreach (var (_, connection) in _connections)
            await connection.DisposeAsync().ConfigureAwait(false);

        _connections.Clear();
        _initLock.Dispose();
    }

    /// <summary>
    /// 异步初始化所有配置的 Redis 实例连接。
    /// 不阻塞调用方，连接在后台建立。
    /// </summary>
    public async Task InitializeAllAsync(CancellationToken ct = default)
    {
        var tasks = _options.Instances.Keys.Select(name => WarmUpConnectionAsync(name, ct));
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    /// <summary>
    /// 异步预热指定 Redis 实例的连接。
    /// 不阻塞调用方，连接在后台建立。
    /// </summary>
    /// <param name="instanceName">Redis 实例名称。</param>
    /// <param name="ct">取消令牌，用于取消预热操作。</param>
    /// <returns>异步任务，用于等待预热完成。</returns>
    private async Task WarmUpConnectionAsync(string instanceName, CancellationToken ct)
    {
        try
        {
            var connection = GetOrCreateConnection(instanceName);
            await connection.ConnectAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "预热 Redis 实例 [{InstanceName}] 连接失败，将在首次使用时重试", instanceName);
        }
    }

    /// <summary>
    /// 获取或创建指定 Redis 实例的连接。
    /// 如果连接不存在，会尝试创建并初始化。
    /// </summary>
    /// <param name="instanceName">Redis 实例名称。</param>
    /// <returns>Redis 连接实例。</returns>
    private CacheMemoryConnection GetOrCreateConnection(string instanceName)
    {
        if (_connections.TryGetValue(instanceName, out var existing))
            return existing;

        _initLock.Wait();
        try
        {
            if (_connections.TryGetValue(instanceName, out existing))
                return existing;

            if (!_options.Instances.TryGetValue(instanceName, out var instanceOptions))
                throw new InvalidOperationException(
                    $"未找到 Redis 实例配置: '{instanceName}'。已注册的实例: {string.Join(", ", _options.Instances.Keys)}");

            var retryOptions = instanceOptions.Retry ?? _options.Retry;

            var connection = new CacheMemoryConnection(
                instanceOptions,
                retryOptions,
                NullLogger<CacheMemoryConnection>.Instance);

            _connections[instanceName] = connection;
            _logger.LogInformation("已注册 Redis 实例 [{InstanceName}]", instanceName);
            return connection;
        }
        finally
        {
            _initLock.Release();
        }
    }

    /// <summary>
    /// 归一化 Redis 实例名称，确保它不为空。
    /// 如果实例名称为空或 null，返回默认实例名称。
    /// </summary>
    /// <param name="instanceName">Redis 实例名称。</param>
    /// <returns>归一化后的实例名称。</returns>
    private static string NormalizeInstanceName(string? instanceName)
    {
        return string.IsNullOrWhiteSpace(instanceName)
            ? CacheMemoryOption.DefaultInstanceName
            : instanceName;
    }

    /// <summary>
    /// 确保连接提供程序未被处置。
    /// 如果已处置，会抛出异常。
    /// </summary>
       private void EnsureNotDisposed()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(RedisConnectionProvider));
    }
}
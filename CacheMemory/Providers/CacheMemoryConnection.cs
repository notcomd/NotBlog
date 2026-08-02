using CacheMemory.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using StackExchange.Redis;

namespace CacheMemory.Providers;

/// <summary>
/// Redis 连接管理器。
/// 负责创建、维持和自动恢复与 Redis 服务器的连接。
/// 支持心跳检测和指数退避重连策略。
/// </summary>
public sealed class CacheMemoryConnection(
    RedisInstanceOptions options,
    RetryOptions retryOptions,
    ILogger<CacheMemoryConnection>? logger = null)
    : IAsyncDisposable
{
    private readonly ConfigurationOptions? _configOptions = options.ToConfigurationOptions();
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private readonly ILogger<CacheMemoryConnection> _logger = logger ?? NullLogger<CacheMemoryConnection>.Instance;
    private readonly RedisInstanceOptions _options = options ?? throw new ArgumentNullException(nameof(options));
    private readonly RetryOptions _retryOptions = retryOptions ?? throw new ArgumentNullException(nameof(retryOptions));


    private IConnectionMultiplexer? _connection;
    private volatile bool _isDisposed;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        var connection = Interlocked.Exchange(ref _connection, null);
        if (connection != null)
        {
            UnregisterConnectionEvents(connection);
            await connection.CloseAsync().ConfigureAwait(false);
            connection.Dispose();
        }

        _connectionLock.Dispose();
    }

    /// <summary>
    /// 获取或注册连接状态变化事件。
    /// </summary>
    public event EventHandler<ConnectionFailedEventArgs>? ConnectionFailed;

    public event EventHandler<ConnectionFailedEventArgs>? ConnectionRestored;

    /// <summary>
    /// 获取当前连接的多路复用器。如果尚未连接，将自动尝试连接。
    /// </summary>
    public IConnectionMultiplexer GetConnection()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(CacheMemoryConnection));

        if (_connection is { IsConnected: true })
            return _connection;

        _connectionLock.Wait();
        try
        {
            // 双重检查，避免并发下重复建连
            if (_connection is { IsConnected: true })
                return _connection;

            var newConnection = ConnectWithRetry();
            ReplaceConnection(newConnection);
            return newConnection;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    /// <summary>
    /// 异步尝试建立连接。
    /// </summary>
    public async Task<IConnectionMultiplexer> ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(CacheMemoryConnection));

        if (_connection is { IsConnected: true })
            return _connection;

        await _connectionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // 双重检查
            if (_connection is { IsConnected: true })
                return _connection;

            var newConnection = await ConnectWithRetryAsync(cancellationToken).ConfigureAwait(false);
            ReplaceConnection(newConnection);
            return newConnection;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    /// <summary>
    /// 带指数退避的同步连接。
    /// </summary>
    private IConnectionMultiplexer ConnectWithRetry()
    {
        for (var attempt = 0;; attempt++)
        {
            try
            {
                var conn = ConnectionMultiplexer.Connect(_configOptions!);
                _logger.LogInformation("Redis 连接成功: {ConnectionString}",
                    MaskConnectionString(_options.ConnectionString));
                return conn;
            }
            catch (Exception ex) when (attempt < _retryOptions.MaxRetryCount)
            {
                var delay = _retryOptions.GetDelay(attempt);
                _logger.LogWarning(ex,
                    "Redis 连接失败（第 {Attempt}/{MaxRetry} 次），{DelayMs}ms 后重试",
                    attempt + 1, _retryOptions.MaxRetryCount, delay.TotalMilliseconds);
                Thread.Sleep(delay);
            }
        }
    }

    /// <summary>
    /// 带指数退避的异步连接。
    /// </summary>
    private async Task<IConnectionMultiplexer> ConnectWithRetryAsync(CancellationToken ct)
    {
        for (var attempt = 0;; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var conn = await ConnectionMultiplexer.ConnectAsync(_configOptions!).ConfigureAwait(false);
                _logger.LogInformation("Redis 异步连接成功: {ConnectionString}",
                    MaskConnectionString(_options.ConnectionString));
                return conn;
            }
            catch (Exception ex) when (attempt < _retryOptions.MaxRetryCount)
            {
                var delay = _retryOptions.GetDelay(attempt);
                _logger.LogWarning(ex,
                    "Redis 异步连接失败（第 {Attempt}/{MaxRetry} 次），{DelayMs}ms 后重试",
                    attempt + 1, _retryOptions.MaxRetryCount, delay.TotalMilliseconds);

                try
                {
                    await Task.Delay(delay, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
            }
        }
    }

    /// <summary>
    /// 订阅连接事件，实现自动重连。
    /// </summary>
    private void RegisterConnectionEvents(IConnectionMultiplexer connection)
    {
        connection.ConnectionFailed += OnConnectionFailed;
        connection.ConnectionRestored += OnConnectionRestored;
        connection.InternalError += OnInternalError;
    }

    /// <summary>
    /// 注销连接事件订阅，防止旧连接泄漏与重复回调。
    /// </summary>
    private void UnregisterConnectionEvents(IConnectionMultiplexer connection)
    {
        connection.ConnectionFailed -= OnConnectionFailed;
        connection.ConnectionRestored -= OnConnectionRestored;
        connection.InternalError -= OnInternalError;
    }

    /// <summary>
    /// 用新连接替换旧连接：先注销并释放旧连接，再注册新连接事件。
    /// 调用方必须持有 <see cref="_connectionLock"/>。
    /// </summary>
    private void ReplaceConnection(IConnectionMultiplexer newConnection)
    {
        var oldConnection = _connection;
        if (oldConnection != null)
        {
            UnregisterConnectionEvents(oldConnection);
            DisposeConnection(oldConnection);
        }

        _connection = newConnection;
        RegisterConnectionEvents(newConnection);
    }

    private void DisposeConnection(IConnectionMultiplexer connection)
    {
        try
        {
            connection.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "释放旧 Redis 连接失败");
        }
    }

    private void OnConnectionFailed(object? sender, ConnectionFailedEventArgs args)
    {
        _logger.LogWarning("Redis 连接失败: {FailureType}, {Exception}", args.FailureType, args.Exception?.Message);
        ConnectionFailed?.Invoke(this, args);
    }

    private void OnConnectionRestored(object? sender, ConnectionFailedEventArgs args)
    {
        _logger.LogInformation("Redis 连接已恢复: {FailureType}", args.FailureType);
        ConnectionRestored?.Invoke(this, args);
    }

    private void OnInternalError(object? sender, InternalErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Redis 内部错误: {Origin}", args.Origin);
    }

    /// <summary>
    /// 掩码连接字符串中的密码，用于安全日志输出。
    /// </summary>
    private static string MaskConnectionString(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            return "(empty)";

        // 简单的密码掩码：替换 password=xxx 为 password=***
        const string passwordPattern = "password=";
        var idx = connectionString.IndexOf(passwordPattern, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return connectionString;

        var start = idx + passwordPattern.Length;
        var end = connectionString.IndexOf(',', start);
        if (end < 0) end = connectionString.Length;

        return string.Concat(connectionString.AsSpan(0, start), "***", connectionString.AsSpan(end));
    }
}

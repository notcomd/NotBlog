using System.Diagnostics;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Notcomd.EventBus.EventBus;

/// <summary>
/// RabbitMQ 连接管理（纯异步，无 sync-over-async）
/// 
/// 使用 IConnectionFactory（由 DI 注册），支持 Aspire.RabbitMQ.Client 或手动配置。
/// </summary>
public class RabbitMqConnection : IAsyncDisposable
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly Lock _syncRoot = new();
    private IConnection? _connection;
    private bool _disposed;

    // S-19：共享连接引用计数。RabbitMqEventBus 与 RabbitMqRequestBus 共用同一连接实例，
    // 任一使用方 Dispose 不应销毁其他使用方的连接，仅在引用计数归零时才真正断开。
    private int _refCount;
    private readonly SemaphoreSlim _reconnectLock = new(1, 1);

    public RabbitMqConnection(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    /// <summary>增加一个使用方引用（连接恢复后不会因他人 Dispose 而失效）</summary>
    public void AddRef()
    {
        lock (_syncRoot)
        {
            _refCount++;
        }
    }

    /// <summary>释放一个使用方引用；引用归零时才真正断开连接</summary>
    public void ReleaseRef()
    {
        bool shouldDispose;
        lock (_syncRoot)
        {
            _refCount = Math.Max(0, _refCount - 1);
            shouldDispose = _refCount == 0;
        }

        if (shouldDispose)
        {
            _ = DisposeAsync();
        }
    }

    public bool IsConnected => _connection != null && _connection.IsOpen && !_disposed;

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        if (_connection != null) await _connection.DisposeAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// 创建 Channel（异步版本，替代 CreateModel）
    /// </summary>
    public virtual async Task<IChannel> CreateChannelAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
            await TryConnectAsync(cancellationToken).ConfigureAwait(false);
        return await (_connection ?? throw new InvalidOperationException("RabbitMQ connection is not available"))
            .CreateChannelAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// 异步连接（修复原版 sync-over-async 死锁风险）
    /// </summary>
    public virtual async Task<bool> TryConnectAsync(CancellationToken cancellationToken = default)
    {
        lock (_syncRoot)
        {
            if (IsConnected) return true;
        }

        await _reconnectLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // 双重检查：等待锁期间可能已被其他调用方恢复
            lock (_syncRoot)
            {
                if (IsConnected) return true;
            }

            var connection = await _connectionFactory
                .CreateConnectionAsync(cancellationToken)
                .ConfigureAwait(false);

            lock (_syncRoot)
            {
                if (_disposed)
                {
                    _ = connection.DisposeAsync();
                    return false;
                }

                // 替换旧连接（断线重建）；旧连接由 RabbitMQ.Client 内部清理
                var old = _connection;
                _connection = connection;
                if (old is not null)
                {
                    _ = old.DisposeAsync();
                }
            }

            connection.ConnectionShutdownAsync += OnConnectionShutdown;
            connection.CallbackExceptionAsync += OnCallbackException;
            connection.ConnectionBlockedAsync += OnConnectionBlocked;
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Evenbus] RabbitMQ 连接失败: {ex.Message}");
            return false;
        }
        finally
        {
            _reconnectLock.Release();
        }
    }

    private Task OnConnectionShutdown(object? sender, ShutdownEventArgs e)
    {
        if (_disposed) return Task.CompletedTask;
        _ = TryConnectAsync();
        return Task.CompletedTask;
    }

    private Task OnCallbackException(object? sender, CallbackExceptionEventArgs e)
    {
        if (_disposed) return Task.CompletedTask;
        _ = TryConnectAsync();
        return Task.CompletedTask;
    }

    private Task OnConnectionBlocked(object? sender, ConnectionBlockedEventArgs e)
    {
        if (_disposed) return Task.CompletedTask;
        _ = TryConnectAsync();
        return Task.CompletedTask;
    }

    public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();
}

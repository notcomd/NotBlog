using System.Diagnostics;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Notcomd.Evenbus.EventBus;

/// <summary>
/// RabbitMQ 连接管理（纯异步，无 sync-over-async）
/// </summary>
public class RabbitMqConnection : IAsyncDisposable
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly Lock _syncRoot = new();
    private IConnection? _connection;
    private bool _disposed;

    public RabbitMqConnection(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
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
    public async Task<IChannel> CreateChannelAsync(CancellationToken cancellationToken = default)
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
    public async Task<bool> TryConnectAsync(CancellationToken cancellationToken = default)
    {
        lock (_syncRoot)
        {
            if (IsConnected) return true;
        }

        try
        {
            var connection = await _connectionFactory
                .CreateConnectionAsync(cancellationToken)
                .ConfigureAwait(false);

            lock (_syncRoot)
            {
                if (_disposed) return false;
                _connection = connection;
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
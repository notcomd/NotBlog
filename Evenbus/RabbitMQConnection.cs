using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Notcomd.Evenbus;

public class RabbitMqConnection(IConnectionFactory connectionFactory)
{
    private readonly Lock _syncRoot = new();
    private IConnection? _connection;
    private bool _disposed;

    public bool Isconnected => _connection != null && _connection.IsOpen && !_disposed;

    public async Task<IChannel> CreateModel()
    {
        if (!Isconnected)
            throw new InvalidOperationException("no RabbitMQ connections are available to perform this action");
        return  await _connection!.CreateChannelAsync() ?? throw new InvalidOperationException();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _connection?.Dispose();
    }

    public bool TryConnect()
    {
        lock (_syncRoot)
        {
            _connection = connectionFactory.CreateConnectionAsync().GetAwaiter().GetResult();
            if (Isconnected)
            {
                _connection.ConnectionShutdownAsync += OnConnectionShutdown;
                _connection.CallbackExceptionAsync += OnCallbackException;
                _connection.ConnectionBlockedAsync += OnConnectionBlocked;
                return true;
            }

            return false;
        }
    }

    private Task OnConnectionBlocked(object? sender, ConnectionBlockedEventArgs e)
    {
        if (_disposed)
            return Task.CompletedTask;
        TryConnect();
        return Task.CompletedTask;
    }

    private Task OnCallbackException(object? sender, CallbackExceptionEventArgs e)
    {
        if (_disposed) return Task.CompletedTask;
        TryConnect();
        return Task.CompletedTask;
    }

    private Task OnConnectionShutdown(object? sender, ShutdownEventArgs e)
    {
        if (_disposed) return Task.CompletedTask;
        TryConnect();
        return Task.CompletedTask;
    }
}
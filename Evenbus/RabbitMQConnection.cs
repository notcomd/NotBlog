using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Notcomd.Evenbus;

public class RabbitMqConnection(IConnectionFactory connectionFactory)
{
    private readonly Lock _syncRoot = new();
    private IConnection? _connection;
    private bool _disposed;

    public bool Isconnected => _connection != null && _connection.IsOpen && !_disposed;

    public IModel CreateModel()
    {
        if (!Isconnected)
            throw new InvalidOperationException("no RabbitMQ connections are available to perform this action");
        return _connection?.CreateModel();
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
            _connection = connectionFactory.CreateConnection();
            if (Isconnected)
            {
                _connection.ConnectionShutdown += OnConnectionShutdown;
                _connection.CallbackException += OnCallbackException;
                _connection.ConnectionBlocked += OnConnectionBlocked;
                return true;
            }

            return false;
        }
    }

    private void OnConnectionBlocked(object? sender, ConnectionBlockedEventArgs e)
    {
        if (_disposed) return;
        TryConnect();
    }

    private void OnCallbackException(object? sender, CallbackExceptionEventArgs e)
    {
        if (_disposed) return;
        TryConnect();
    }

    private void OnConnectionShutdown(object? sender, ShutdownEventArgs e)
    {
        if (_disposed) return;
        TryConnect();
    }
}
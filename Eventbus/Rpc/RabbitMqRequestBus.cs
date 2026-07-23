using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Notcomd.EventBus.EventBus;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Notcomd.EventBus.Rpc;

/// <summary>
/// RabbitMQ RPC 实现（基于临时回复队列 + CorrelationId）
/// 
/// 使用方式:
///   服务端: 实现 IRequestHandler&lt;TReq, TRes&gt; 并注册到 DI
///   客户端: await requestBus.SendAsync&lt;MyRequest, MyResponse&gt;(request)
/// </summary>
public class RabbitMqRequestBus : IRequestBus, IAsyncDisposable
{
    private readonly RabbitMqConnection _connection;
    private readonly ILogger<RabbitMqRequestBus>? _logger;
    private readonly IntegrationEventRabbitMqOptions _options;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _pendingRequests = new();
    private readonly IServiceScopeFactory _scopeFactory;
    private IChannel? _channel;
    private string? _replyQueueName;

    public RabbitMqRequestBus(
        RabbitMqConnection connection,
        IntegrationEventRabbitMqOptions options,
        IServiceScopeFactory? scopeFactory = null,
        ILogger<RabbitMqRequestBus>? logger = null)
    {
        _connection = connection;
        _options = options;
        _scopeFactory = scopeFactory!;
        _logger = logger;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var (_, tcs) in _pendingRequests)
            tcs.TrySetCanceled();
        _pendingRequests.Clear();

        if (_channel != null)
            await _channel.DisposeAsync().ConfigureAwait(false);
        await _connection.DisposeAsync().ConfigureAwait(false);
    }

    public async Task<TResponse> SendAsync<TRequest, TResponse>(TRequest request,
        CancellationToken cancellationToken = default) where TRequest : class where TResponse : class
    {
        return await SendAsync<TRequest, TResponse>(request,
            TimeSpan.FromSeconds(_options.RequestTimeoutSeconds), cancellationToken).ConfigureAwait(false);
    }

    public async Task<TResponse> SendAsync<TRequest, TResponse>(TRequest request, TimeSpan timeout,
        CancellationToken cancellationToken = default) where TRequest : class where TResponse : class
    {
        if (_channel == null)
            throw new InvalidOperationException("RPC 未初始化，请先调用 InitializeAsync()");

        var correlationId = Guid.NewGuid().ToString("N");
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingRequests[correlationId] = tcs;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        // 注册超时取消
        _ = Task.Delay(timeout).ContinueWith(_ =>
        {
            if (_pendingRequests.TryRemove(correlationId, out var existing))
                existing.TrySetException(new TimeoutException(
                    $"RPC 请求超时 ({timeout.TotalSeconds}s): {typeof(TRequest).Name}"));
        });

        try
        {
            var routingKey = typeof(TRequest).Name;
            var jsonOptions = new JsonSerializerOptions { WriteIndented = false };
            var body = JsonSerializer.SerializeToUtf8Bytes(request, jsonOptions);

            var props = new BasicProperties
            {
                CorrelationId = correlationId,
                ReplyTo = _replyQueueName,
                Persistent = true
            };

            await _channel.BasicPublishAsync(
                $"{_options.ExchangeName}.rpc", routingKey,
                mandatory: true, props, body).ConfigureAwait(false);

            _logger?.LogDebug("[Evenbus-RPC] 发送请求: {Type}, CorrelationId: {Id}",
                routingKey, correlationId);

            var responseJson = await tcs.Task.ConfigureAwait(false);
            return JsonSerializer.Deserialize<TResponse>(responseJson, jsonOptions)
                   ?? throw new JsonException("RPC 响应反序列化结果为 null");
        }
        finally
        {
            _pendingRequests.TryRemove(correlationId, out _);
        }
    }

    /// <summary>
    /// 初始化 RPC 通道和回复队列
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _channel = await _connection.CreateChannelAsync(cancellationToken).ConfigureAwait(false);

        // 声明 RPC Exchange
        await _channel.ExchangeDeclareAsync($"{_options.ExchangeName}.rpc", "direct", durable: true)
            .ConfigureAwait(false);

        // 声明临时回复队列
        var declareResult = await _channel.QueueDeclareAsync(exclusive: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        _replyQueueName = declareResult.QueueName;

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += OnReplyReceived;
        await _channel.BasicConsumeAsync(_replyQueueName, autoAck: true, consumer)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// 注册 RPC 服务端处理器（在接收方服务调用）
    /// </summary>
    public async Task RegisterHandlerAsync<TRequest, TResponse>(
        CancellationToken cancellationToken = default) where TRequest : class where TResponse : class
    {
        if (_channel == null)
            throw new InvalidOperationException("RPC 未初始化");

        var routingKey = typeof(TRequest).Name;
        var queueName = $"rpc.{routingKey}";

        await _channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        await _channel.QueueBindAsync(queueName, $"{_options.ExchangeName}.rpc", routingKey,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (sender, args) =>
        {
            await HandleRpcRequestAsync<TRequest, TResponse>(args).ConfigureAwait(false);
        };
        await _channel.BasicConsumeAsync(queueName, autoAck: false, consumer)
            .ConfigureAwait(false);

        _logger?.LogInformation("[Evenbus-RPC] 注册处理器: {RequestType} → {ResponseType}, Queue: {Queue}",
            typeof(TRequest).Name, typeof(TResponse).Name, queueName);
    }

    private async Task HandleRpcRequestAsync<TRequest, TResponse>(BasicDeliverEventArgs args)
        where TRequest : class where TResponse : class
    {
        TResponse? response = null;
        bool success = false;

        try
        {
            var requestBody = Encoding.UTF8.GetString(args.Body.Span);
            var request = JsonSerializer.Deserialize<TRequest>(requestBody);

            if (request != null && _scopeFactory != null)
            {
                using var scope = _scopeFactory.CreateScope();
                var handler = scope.ServiceProvider.GetService<IRequestHandler<TRequest, TResponse>>();
                if (handler != null)
                {
                    response = await handler.HandleAsync(request).ConfigureAwait(false);
                    success = true;
                }
                else
                {
                    _logger?.LogError("[Evenbus-RPC] 未找到处理器: {Type}",
                        typeof(IRequestHandler<TRequest, TResponse>).Name);
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[Evenbus-RPC] 处理请求失败");
        }
        finally
        {
            // 无论成功与否都回复（错误时回复异常信息）
            await SendReplyAsync(args.BasicProperties, response, success).ConfigureAwait(false);
            await _channel!.BasicAckAsync(args.DeliveryTag, multiple: false).ConfigureAwait(false);
        }
    }

    private async Task SendReplyAsync<TResponse>(IReadOnlyBasicProperties requestProps, TResponse? response,
        bool success)
    {
        var replyProps = new BasicProperties
        {
            CorrelationId = requestProps.CorrelationId
        };

        var jsonOptions = new JsonSerializerOptions { WriteIndented = false };
        var body = success
            ? JsonSerializer.SerializeToUtf8Bytes(response!, jsonOptions)
            : JsonSerializer.SerializeToUtf8Bytes(new RpcErrorResponse
            {
                Success = false,
                Error = "请求处理失败"
            }, jsonOptions);

        await _channel!.BasicPublishAsync("", requestProps.ReplyTo, false, replyProps, body)
            .ConfigureAwait(false);
    }

    private async Task OnReplyReceived(object sender, BasicDeliverEventArgs args)
    {
        var correlationId = args.BasicProperties.CorrelationId;
        var body = Encoding.UTF8.GetString(args.Body.Span);

        if (_pendingRequests.TryRemove(correlationId, out var tcs))
        {
            tcs.SetResult(body);
        }
        else
        {
            _logger?.LogWarning("[Evenbus-RPC] 收到未知 CorrelationId 的回复: {Id}", correlationId);
        }
    }
}

/// <summary>
/// RPC 错误响应
/// </summary>
internal class RpcErrorResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
}
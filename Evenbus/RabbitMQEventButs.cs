using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Notcomd.Evenbus;

/// <summary>
/// RabbitMQ EventBus 实现（Pub/Sub 模式）
/// 
/// 增强：
/// - 异步初始化（消除 sync-over-async）
/// - 死信队列（DLQ）支持
/// - 自动重试策略
/// - Exchange 类型可配置（Direct/Fanout/Topic）
/// </summary>
public class RabbitMqEventBus : IEventBus, IAsyncDisposable
{
    private readonly string _exchangeName;
    private readonly ILogger<RabbitMqEventBus>? _logger;
    private readonly IntegrationEventRabbitMqOptions _options;
    private readonly string _queueName;
    private readonly RabbitMqConnection _rabbitMqConnection;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly SubscriptionsManager _subscriptionsManager;

    private IChannel? _consumerChannel;

    public RabbitMqEventBus(
        RabbitMqConnection rabbitMqConnection,
        IntegrationEventRabbitMqOptions options,
        string queueName,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<RabbitMqEventBus>? logger = null)
    {
        _rabbitMqConnection = rabbitMqConnection ?? throw new ArgumentNullException(nameof(rabbitMqConnection));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _exchangeName = options.ExchangeName;
        _queueName = queueName;
        _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        _logger = logger;
        _subscriptionsManager = new SubscriptionsManager();
        _subscriptionsManager.OnEventRemoved += SubsManager_OnEventRemoved;
    }

    public async ValueTask DisposeAsync()
    {
        if (_consumerChannel != null)
            await _consumerChannel.DisposeAsync().ConfigureAwait(false);
        _subscriptionsManager.Clear();
        await _rabbitMqConnection.DisposeAsync().ConfigureAwait(false);
    }

    public async Task Publish(string eventName, object? eventData)
    {
        if (!_rabbitMqConnection.IsConnected)
            await _rabbitMqConnection.TryConnectAsync().ConfigureAwait(false);

        await using var channel = await _rabbitMqConnection.CreateChannelAsync().ConfigureAwait(false);
        var exchangeTypeStr = _options.ExchangeType switch
        {
            ExchangeType.Direct => "direct",
            ExchangeType.Fanout => "fanout",
            ExchangeType.Topic => "topic",
            _ => "direct"
        };
        await channel.ExchangeDeclareAsync(_exchangeName, exchangeTypeStr, durable: true).ConfigureAwait(false);

        byte[] body;
        if (eventData == null)
        {
            body = [];
        }
        else
        {
            var jsonOptions = new JsonSerializerOptions { WriteIndented = false };
            body = JsonSerializer.SerializeToUtf8Bytes(eventData, eventData.GetType(), jsonOptions);
        }

        var properties = new BasicProperties { Persistent = true };
        await channel.BasicPublishAsync(_exchangeName, eventName, mandatory: true, properties, body)
            .ConfigureAwait(false);

        _logger?.LogDebug("[Evenbus] 发布事件: {EventName}, 大小: {Size} bytes", eventName, body.Length);
    }

    public async Task Subscribe(string eventName, Type handlerType)
    {
        CheckHandlerType(handlerType);
        DoInternalSubscription(eventName).GetAwaiter().GetResult();
        _subscriptionsManager.AddSubscription(eventName, handlerType);
        StartBasicConsume();
    }

    public Task Unsubscribe(string eventName, Type handlerType)
    {
        CheckHandlerType(handlerType);
        _subscriptionsManager.RemoveSubscription(eventName, handlerType);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 初始化连接和通道（应在应用启动时调用）
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _consumerChannel = await CreateConsumerChannelAsync(cancellationToken).ConfigureAwait(false);
    }

    // ── 内部方法 ──────────────────────────────────────────────

    private async Task<IChannel> CreateConsumerChannelAsync(CancellationToken cancellationToken = default)
    {
        if (!_rabbitMqConnection.IsConnected)
            await _rabbitMqConnection.TryConnectAsync(cancellationToken).ConfigureAwait(false);

        var channel = await _rabbitMqConnection.CreateChannelAsync(cancellationToken).ConfigureAwait(false);

        var exchangeTypeStr = _options.ExchangeType switch
        {
            ExchangeType.Direct => "direct",
            ExchangeType.Fanout => "fanout",
            ExchangeType.Topic => "topic",
            _ => "direct"
        };

        await channel.ExchangeDeclareAsync(_exchangeName, exchangeTypeStr, durable: true).ConfigureAwait(false);

        // 声明主队列 + 死信队列
        var dlqName = $"{_queueName}{_options.DeadLetterQueueSuffix}";
        await channel.QueueDeclareAsync(dlqName, durable: true, exclusive: false, autoDelete: false)
            .ConfigureAwait(false);

        var queueArgs = new Dictionary<string, object>
        {
            ["x-dead-letter-exchange"] = "",
            ["x-dead-letter-routing-key"] = dlqName
        };
        await channel.QueueDeclareAsync(_queueName, durable: true, exclusive: false, autoDelete: false,
                arguments: queueArgs)
            .ConfigureAwait(false);

        channel.CallbackExceptionAsync += OnCallbackException;
        return channel;
    }

    private void StartBasicConsume()
    {
        if (_consumerChannel == null) return;
        var consumer = new AsyncEventingBasicConsumer(_consumerChannel);
        consumer.ReceivedAsync += OnMessageReceived;
        _ = _consumerChannel.BasicConsumeAsync(_queueName, autoAck: false, consumer);
    }

    private async Task OnMessageReceived(object sender, BasicDeliverEventArgs @event)
    {
        var eventName = @event.RoutingKey;
        var message = Encoding.UTF8.GetString(@event.Body.Span);

        if (await ProcessWithRetryAsync(eventName, message, @event, retryCount: 0).ConfigureAwait(false))
        {
            await _consumerChannel!.BasicAckAsync(@event.DeliveryTag, multiple: false).ConfigureAwait(false);
        }
        else
        {
            // 重试耗尽，发送到死信队列（nack + requeue=false 触发 DLQ）
            _logger?.LogError("[Evenbus] 事件处理失败已达最大重试次数: {EventName}, 发送到 DLQ", eventName);
            await _consumerChannel!.BasicNackAsync(@event.DeliveryTag, multiple: false, requeue: false)
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 带重试的事件处理
    /// </summary>
    private async Task<bool> ProcessWithRetryAsync(string eventName, string message,
        BasicDeliverEventArgs deliverEvent, int retryCount)
    {
        try
        {
            await ProcessEventAsync(eventName, message).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "[Evenbus] 事件处理失败 (重试 {Retry}/{Max}): {EventName}",
                retryCount + 1, _options.MaxRetryCount, eventName);

            if (retryCount < _options.MaxRetryCount)
            {
                await Task.Delay(_options.RetryIntervalMs * (retryCount + 1)).ConfigureAwait(false);
                return await ProcessWithRetryAsync(eventName, message, deliverEvent, retryCount + 1)
                    .ConfigureAwait(false);
            }

            return false;
        }
    }

    private async Task ProcessEventAsync(string eventName, string message)
    {
        if (!_subscriptionsManager.HasSubscriptionForEvent(eventName))
        {
            _logger?.LogWarning("[Evenbus] 未找到事件处理器: {EventName}", eventName);
            return;
        }

        var handlers = _subscriptionsManager.GetHandlersForEvent(eventName);
        foreach (var handlerType in handlers)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetService(handlerType) as IIntegrationEventHandler;
            if (handler == null)
                throw new ApplicationException($"无法创建 Handler: {handlerType.Name}");
            await handler.Handler(eventName, message).ConfigureAwait(false);
        }
    }

    private async Task DoInternalSubscription(string eventName)
    {
        if (_subscriptionsManager.HasSubscriptionForEvent(eventName)) return;

        if (!_rabbitMqConnection.IsConnected)
            await _rabbitMqConnection.TryConnectAsync().ConfigureAwait(false);

        if (_consumerChannel != null)
            await _consumerChannel.QueueBindAsync(_queueName, _exchangeName, eventName).ConfigureAwait(false);
    }

    private void SubsManager_OnEventRemoved(object? sender, string eventName)
    {
        _ = SubsManager_OnEventRemovedAsync(eventName);
    }

    private async Task SubsManager_OnEventRemovedAsync(string eventName)
    {
        if (!_rabbitMqConnection.IsConnected)
            await _rabbitMqConnection.TryConnectAsync().ConfigureAwait(false);

        await using var channel = await _rabbitMqConnection.CreateChannelAsync().ConfigureAwait(false);
        await channel.QueueUnbindAsync(_queueName, _exchangeName, eventName).ConfigureAwait(false);

        if (_subscriptionsManager.IsEmpty && _consumerChannel != null)
        {
            await _consumerChannel.CloseAsync().ConfigureAwait(false);
            _consumerChannel = null;
        }
    }

    private Task OnCallbackException(object? sender, CallbackExceptionEventArgs e)
    {
        _logger?.LogWarning(e.Exception, "[Evenbus] Channel 回调异常，尝试重建");
        _ = RebuildChannelAsync();
        return Task.CompletedTask;
    }

    private async Task RebuildChannelAsync()
    {
        try
        {
            _consumerChannel = await CreateConsumerChannelAsync().ConfigureAwait(false);
            // 重新绑定所有已订阅的事件
            foreach (var eventName in _subscriptionsManager.GetEventNames())
            {
                if (_consumerChannel != null)
                    await _consumerChannel.QueueBindAsync(_queueName, _exchangeName, eventName).ConfigureAwait(false);
            }

            StartBasicConsume();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[Evenbus] 重建 Channel 失败");
        }
    }

    private static void CheckHandlerType(Type handlerType)
    {
        if (!typeof(IIntegrationEventHandler).IsAssignableFrom(handlerType))
            throw new ArgumentException(
                $"{handlerType.Name} does not implement {nameof(IIntegrationEventHandler)}", nameof(handlerType));
    }
}
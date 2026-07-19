using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Evenbus.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notcomd.Evenbus.Core;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace Notcomd.Evenbus.EventBus;

/// <summary>
/// RabbitMQ EventBus 实现（Pub/Sub 模式）
/// 
/// 功能：
/// - Polly ResiliencePipeline 重试策略（指数退避，基数可配）
/// - OpenTelemetry 分布式追踪
/// - IHostedService 自动启动消费
/// - 死信队列（DLQ）支持
/// - 多 Handler 支持（KeyedService）
/// - Exchange 类型可配置（Direct/Fanout/Topic）
/// - 消费并发控制（PrefetchCount + MaxConcurrency）
/// - 结构化日志记录
/// </summary>
public sealed class RabbitMqEventBus : IEventBus, IHostedService, IAsyncDisposable
{
    private string ExchangeName => _options.ExchangeName;

    private readonly EventBusOptions _options;
    private readonly ResiliencePipeline _publishPipeline;
    private readonly TextMapPropagator _propagator;
    private readonly ActivitySource _activitySource;
    private readonly string _queueName;
    private readonly EventBusSubscriptionInfo _subscriptionInfo;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RabbitMqEventBus> _logger;
    private readonly RabbitMqConnection _connection;

    private IChannel? _consumerChannel;
    private IChannel? _publishChannel;
    private readonly SemaphoreSlim _publishChannelLock = new(1, 1);
    private SemaphoreSlim? _concurrencyLimiter;
    private int _exchangeDeclared;
    private int _disposed;

    public RabbitMqEventBus(
        RabbitMqConnection connection,
        IOptions<EventBusOptions> options,
        IOptions<EventBusSubscriptionInfo> subscriptionOptions,
        RabbitMQTelemetry telemetry,
        IServiceProvider serviceProvider,
        ILogger<RabbitMqEventBus> logger)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _options = options.Value;
        _subscriptionInfo = subscriptionOptions.Value;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _queueName = _options.SubscriptionClientName;
        _propagator = telemetry.Propagator;
        _activitySource = telemetry.ActivitySource;

        if (_options.MaxConcurrency > 1)
        {
            _concurrencyLimiter = new SemaphoreSlim(_options.MaxConcurrency, _options.MaxConcurrency);
        }

        _publishPipeline = CreateResiliencePipeline(_options);
    }

    // ══════════════════════════════════════════════════
    //  IEventBus
    // ══════════════════════════════════════════════════

    public async Task PublishAsync(IntegrationEvent @event)
    {
        var routingKey = @event.GetType().Name;

        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace("[Evenbus] 创建通道以发布事件: Id={EventId}, Event={EventName}",
                @event.Id, routingKey);
        }

        var channel = await GetPublishChannelAsync().ConfigureAwait(false);

        var body = SerializeMessage(@event);

        // OpenTelemetry: 遵循消息规范创建 Activity
        var activityName = $"{routingKey} publish";

        await _publishPipeline.ExecuteAsync(async _ =>
        {
            using var activity = _activitySource.StartActivity(activityName, ActivityKind.Client);

            ActivityContext contextToInject = activity?.Context
                ?? Activity.Current?.Context
                ?? default;

            var properties = new BasicProperties
            {
                DeliveryMode = DeliveryModes.Persistent
            };

            static void InjectTraceContext(IBasicProperties props, string key, string value)
            {
                props.Headers ??= new Dictionary<string, object>();
                props.Headers[key] = value;
            }

            _propagator.Inject(
                new PropagationContext(contextToInject, Baggage.Current),
                properties,
                InjectTraceContext);

            SetActivityTags(activity, routingKey, "publish");

            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace("[Evenbus] 发布事件: Id={EventId}, Size={Size} bytes",
                    @event.Id, body.Length);
            }

            try
            {
                await channel.BasicPublishAsync(
                    exchange: ExchangeName,
                    routingKey: routingKey,
                    mandatory: true,
                    basicProperties: properties,
                    body: body).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.SetTag("exception.type", ex.GetType().FullName);
                activity?.SetTag("exception.message", ex.Message);
                throw;
            }
        });

        _logger.LogDebug("[Evenbus] 事件已发布: Event={EventName}, Id={EventId}, Size={Size} bytes",
            routingKey, @event.Id, body.Length);
    }

    private async Task<IChannel> GetPublishChannelAsync()
    {
        if (_publishChannel is not null && _exchangeDeclared == 1)
            return _publishChannel;

        await _publishChannelLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_publishChannel is null)
            {
                _publishChannel = await _connection.CreateChannelAsync().ConfigureAwait(false);
            }

            if (Interlocked.CompareExchange(ref _exchangeDeclared, 1, 0) == 0)
            {
                await _publishChannel.ExchangeDeclareAsync(
                    ExchangeName, GetExchangeTypeString(_options.ExchangeType), durable: true)
                    .ConfigureAwait(false);
            }

            return _publishChannel;
        }
        finally
        {
            _publishChannelLock.Release();
        }
    }

    // ══════════════════════════════════════════════════
    //  IHostedService
    // ══════════════════════════════════════════════════

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = StartConsumerAsync(cancellationToken);
        return Task.CompletedTask;
    }

    private async Task StartConsumerAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("[Evenbus] 正在启动 RabbitMQ 消费者...");

            if (!_connection.IsConnected)
                await _connection.TryConnectAsync(cancellationToken).ConfigureAwait(false);

            if (!_connection.IsConnected)
            {
                _logger.LogError("[Evenbus] RabbitMQ 连接失败，消费者未启动");
                return;
            }

            _logger.LogInformation("[Evenbus] 创建消费者通道");

            _consumerChannel = await _connection.CreateChannelAsync(cancellationToken)
                .ConfigureAwait(false);

            _consumerChannel.CallbackExceptionAsync += (_, ea) =>
            {
                _logger.LogWarning(ea.Exception, "[Evenbus] 消费者通道回调异常");
                return Task.CompletedTask;
            };

            var exchangeTypeStr = GetExchangeTypeString(_options.ExchangeType);

            await _consumerChannel.ExchangeDeclareAsync(ExchangeName, exchangeTypeStr, durable: true,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            // 声明死信队列
            var dlqName = $"{_queueName}{_options.DeadLetterQueueSuffix}";
            await _consumerChannel.QueueDeclareAsync(dlqName, durable: true, exclusive: false,
                autoDelete: false, cancellationToken: cancellationToken).ConfigureAwait(false);

            var queueArgs = new Dictionary<string, object>
            {
                ["x-dead-letter-exchange"] = "",
                ["x-dead-letter-routing-key"] = dlqName
            };

            await _consumerChannel.QueueDeclareAsync(_queueName, durable: true, exclusive: false,
                autoDelete: false, arguments: queueArgs, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            // 设置 QoS（预取数量）
            if (_options.PrefetchCount > 0)
            {
                await _consumerChannel.BasicQosAsync(0, _options.PrefetchCount, false)
                    .ConfigureAwait(false);
            }

            // 绑定所有已注册的事件
            foreach (var (eventName, _) in _subscriptionInfo.EventTypes)
            {
                await _consumerChannel.QueueBindAsync(_queueName, ExchangeName, eventName,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                _logger.LogDebug("[Evenbus] 队列绑定: Queue={Queue}, Event={Event}",
                    _queueName, eventName);
            }

            // 启动消费
            var consumer = new AsyncEventingBasicConsumer(_consumerChannel);
            consumer.ReceivedAsync += OnMessageReceived;

            await _consumerChannel.BasicConsumeAsync(_queueName, autoAck: false, consumer)
                .ConfigureAwait(false);

            _logger.LogInformation(
                "[Evenbus] 消费者已启动，队列: {Queue}, 事件数: {EventCount}, 预取: {Prefetch}, 并发: {Concurrency}",
                _queueName, _subscriptionInfo.EventTypes.Count, _options.PrefetchCount, _options.MaxConcurrency);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Evenbus] 启动 RabbitMQ 消费者失败");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[Evenbus] 正在停止消费者...");
        return Task.CompletedTask;
    }

    // ══════════════════════════════════════════════════
    //  IAsyncDisposable
    // ══════════════════════════════════════════════════

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        if (_publishChannel != null)
            await _publishChannel.DisposeAsync().ConfigureAwait(false);

        if (_consumerChannel != null)
            await _consumerChannel.DisposeAsync().ConfigureAwait(false);

        _publishChannelLock.Dispose();
        _concurrencyLimiter?.Dispose();

        await _connection.DisposeAsync().ConfigureAwait(false);

        _logger.LogDebug("[Evenbus] 已释放资源");
    }

    // ══════════════════════════════════════════════════
    //  消息消费
    // ══════════════════════════════════════════════════

    private async Task OnMessageReceived(object sender, BasicDeliverEventArgs eventArgs)
    {
        // 并发限制
        if (_concurrencyLimiter != null)
            await _concurrencyLimiter.WaitAsync().ConfigureAwait(false);

        try
        {
            await ProcessReceivedMessageAsync(eventArgs).ConfigureAwait(false);
        }
        finally
        {
            _concurrencyLimiter?.Release();
        }
    }

    private async Task ProcessReceivedMessageAsync(BasicDeliverEventArgs eventArgs)
    {
        // 从消息头提取分布式追踪上下文
        static IEnumerable<string> ExtractTraceContext(IReadOnlyBasicProperties props, string key)
        {
            if (props.Headers != null && props.Headers.TryGetValue(key, out var value))
            {
                var bytes = value as byte[];
                return [Encoding.UTF8.GetString(bytes)];
            }
            return [];
        }

        var parentContext = _propagator.Extract(
            default, eventArgs.BasicProperties, ExtractTraceContext);

        Baggage.Current = parentContext.Baggage;

        var activityName = $"{eventArgs.RoutingKey} receive";

        using var activity = _activitySource.StartActivity(
            activityName, ActivityKind.Client, parentContext.ActivityContext);

        SetActivityTags(activity, eventArgs.RoutingKey, "receive");

        var eventName = eventArgs.RoutingKey;
        var message = Encoding.UTF8.GetString(eventArgs.Body.Span);
        var deliveryTag = eventArgs.DeliveryTag;

        _logger.LogDebug(
            "[Evenbus] 收到消息: Event={EventName}, DeliveryTag={DeliveryTag}, Size={Size} bytes",
            eventName, deliveryTag, eventArgs.Body.Length);

        var sw = Stopwatch.StartNew();

        try
        {
            activity?.SetTag("message", message);

            await ProcessEventAsync(eventName, message).ConfigureAwait(false);

            await _consumerChannel!.BasicAckAsync(deliveryTag, multiple: false)
                .ConfigureAwait(false);

            sw.Stop();
            _logger.LogInformation(
                "[Evenbus] 消息处理成功: Event={EventName}, DeliveryTag={DeliveryTag}, 耗时={Elapsed}ms",
                eventName, deliveryTag, (int)sw.Elapsed.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            var dlqName = $"{_queueName}{_options.DeadLetterQueueSuffix}";

            _logger.LogError(ex,
                "[Evenbus] 消息处理失败，已投递到死信队列(DLQ={DlqName})。" +
                "Event={EventName}, DeliveryTag={DeliveryTag}, 消息预览={MessagePreview}",
                dlqName, eventName, deliveryTag, TruncateMessage(message));

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("exception.type", ex.GetType().FullName);
            activity?.SetTag("exception.message", ex.Message);

            // nack + requeue=false 触发 DLQ
            await _consumerChannel!.BasicNackAsync(deliveryTag, multiple: false, requeue: false)
                .ConfigureAwait(false);
        }
    }

    private async Task ProcessEventAsync(string eventName, string message)
    {
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace("[Evenbus] 处理事件: {EventName}", eventName);
        }

        await using var scope = _serviceProvider.CreateAsyncScope();

        if (!_subscriptionInfo.EventTypes.TryGetValue(eventName, out var eventType))
        {
            _logger.LogWarning("[Evenbus] 未找到事件类型: {EventName}", eventName);
            return;
        }

        var integrationEvent = DeserializeMessage(message, eventType);

        var handlers = scope.ServiceProvider
            .GetKeyedServices<IIntegrationEventHandler>(eventType);

        var handlerSw = Stopwatch.StartNew();
        int handlerCount = 0;

        foreach (var handler in handlers)
        {
            await handler.Handler(integrationEvent).ConfigureAwait(false);
            handlerCount++;
        }

        handlerSw.Stop();

        if (handlerCount > 0)
        {
            _logger.LogDebug("[Evenbus] 事件已分发: Event={EventName}, HandlerCount={Count}, 耗时={Elapsed}ms",
                eventName, handlerCount, (int)handlerSw.Elapsed.TotalMilliseconds);
        }
    }

    // ══════════════════════════════════════════════════
    //  序列化 / 反序列化
    // ══════════════════════════════════════════════════

    [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode",
        Justification = "JsonSerializer.IsReflectionEnabledByDefault 确保不使用反射")]
    [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode",
        Justification = "同上")]
    private byte[] SerializeMessage(IntegrationEvent @event)
    {
        return JsonSerializer.SerializeToUtf8Bytes(
            @event, @event.GetType(), _subscriptionInfo.JsonSerializerOptions);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode",
        Justification = "JsonSerializer.IsReflectionEnabledByDefault 确保不使用反射")]
    [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode",
        Justification = "同上")]
    private IntegrationEvent DeserializeMessage(string message, Type eventType)
    {
        return JsonSerializer.Deserialize(message, eventType, _subscriptionInfo.JsonSerializerOptions)
            as IntegrationEvent
            ?? throw new JsonException($"无法反序列化事件: {eventType.Name}");
    }

    // ══════════════════════════════════════════════════
    //  Helper
    // ══════════════════════════════════════════════════

    private static ResiliencePipeline CreateResiliencePipeline(EventBusOptions options)
    {
        var retryOptions = new RetryStrategyOptions
        {
            ShouldHandle = new PredicateBuilder()
                .Handle<BrokerUnreachableException>()
                .Handle<SocketException>(),
            MaxRetryAttempts = options.RetryCount,
            DelayGenerator = context =>
                ValueTask.FromResult((TimeSpan?)TimeSpan.FromSeconds(
                    Math.Pow(options.RetryBackoffBase, context.AttemptNumber)))
        };

        return new ResiliencePipelineBuilder()
            .AddRetry(retryOptions)
            .Build();
    }

    private static string GetExchangeTypeString(ExchangeType type) => type switch
    {
        ExchangeType.Direct => "direct",
        ExchangeType.Fanout => "fanout",
        ExchangeType.Topic => "topic",
        _ => "direct"
    };

    private static void SetActivityTags(Activity? activity, string routingKey, string operation)
    {
        if (activity is null) return;

        activity.SetTag("messaging.system", "rabbitmq");
        activity.SetTag("messaging.destination_kind", "queue");
        activity.SetTag("messaging.operation", operation);
        activity.SetTag("messaging.destination.name", routingKey);
        activity.SetTag("messaging.rabbitmq.routing_key", routingKey);
    }

    private static string TruncateMessage(string message, int maxLength = 500)
    {
        if (string.IsNullOrEmpty(message)) return "(empty)";
        return message.Length <= maxLength
            ? message
            : message[..maxLength] + "...(truncated)";
    }
}

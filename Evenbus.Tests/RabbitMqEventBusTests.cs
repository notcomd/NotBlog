using System.Reflection;
using System.Text;
using System.Text.Json;
using Evenbus.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Notcomd.Evenbus;
using Notcomd.Evenbus.Core;
using Notcomd.Evenbus.EventBus;
using Notcomd.Evenbus.Extension;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace Evenbus.Tests;

// ════════════════════════════════════════════════════════════════
//  模拟用的 IntegrationEvent 子类
// ════════════════════════════════════════════════════════════════

/// <summary>模拟「用户注册成功」集成事件</summary>
public record UserRegisteredEvent(
    Guid UserId,
    string UserName,
    string Email,
    DateTime RegisteredAt
) : IntegrationEvent;

/// <summary>模拟「订单已创建」集成事件（无参构造模式）</summary>
public record OrderCreatedEvent : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public decimal Amount { get; init; }
    public string CustomerName { get; init; } = string.Empty;
}

// ════════════════════════════════════════════════════════════════
//  模拟 Handler
// ════════════════════════════════════════════════════════════════

[EvenBusName("UserRegistered")]
public class UserRegisteredHandler(ILogger<UserRegisteredHandler> logger)
    : JsonIntegrationEventHandler<UserRegisteredEvent>
{
    public override Task Handler(UserRegisteredEvent @event)
    {
        logger.LogInformation("[Test] 收到用户注册集成事件: {UserId}, {UserName}",
            @event.UserId, @event.UserName);
        return Task.CompletedTask;
    }
}

[EvenBusName("OrderPlaced")]
public class OrderCreatedHandler : JsonIntegrationEventHandler<OrderCreatedEvent>
{
    public static bool WasCalled { get; set; }
    public static OrderCreatedEvent? LastEvent { get; set; }

    public override Task Handler(OrderCreatedEvent @event)
    {
        WasCalled = true;
        LastEvent = @event;
        return Task.CompletedTask;
    }

    public static void Reset() { WasCalled = false; LastEvent = null; }
}

/// <summary>第二个 OrderCreated Handler（测试多 Handler）</summary>
[EvenBusName("OrderPlaced")]
public class OrderAuditLogHandler(ILogger<OrderAuditLogHandler> logger)
    : JsonIntegrationEventHandler<OrderCreatedEvent>
{
    public override Task Handler(OrderCreatedEvent @event)
    {
        logger.LogInformation("[Audit] 审计日志: 订单 {OrderId} 已创建", @event.OrderId);
        return Task.CompletedTask;
    }
}

/// <summary>模拟 Handler 的工厂方法：用于测试 DI 注册和 KeyedService</summary>
public class HandlerRegistrationHelper
{
    public static List<HandlerRegistration> GetMockRegistrations() =>
    [
        new(typeof(UserRegisteredEvent), typeof(UserRegisteredHandler)),
        new(typeof(OrderCreatedEvent), typeof(OrderCreatedHandler)),
    ];
}

/// <summary>用于单元测试的 Fake RabbitMqConnection，直接返回预设的 IChannel</summary>
internal class FakeRabbitMqConnection(IConnectionFactory connectionFactory, IChannel channel)
    : RabbitMqConnection(connectionFactory)
{
    private readonly IChannel _channel = channel;

    public override Task<IChannel> CreateChannelAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_channel);
}

// ════════════════════════════════════════════════════════════════
//  Stub IChannel — 用于单元测试，捕获 BasicPublishAsync 调用
// ════════════════════════════════════════════════════════════════

internal class StubChannel : IChannel
{
    public BasicProperties? LastPublishedProperties { get; private set; }
    public ReadOnlyMemory<byte> LastPublishedBody { get; private set; }
    public int PublishCallCount { get; private set; }
    public bool ShouldThrowBrokerUnreachable { get; set; }
    public int ThrowCount { get; set; } = -1; // -1 = always throw

    private int _currentCall;

    public ValueTask BasicPublishAsync<TBasicProperties>(
        string exchange, string routingKey, bool mandatory,
        TBasicProperties basicProperties, ReadOnlyMemory<byte> body,
        CancellationToken cancellationToken = default)
        where TBasicProperties : IReadOnlyBasicProperties, IAmqpHeader
    {
        _currentCall++;
        if (ShouldThrowBrokerUnreachable && (ThrowCount == -1 || _currentCall <= ThrowCount))
        {
            PublishCallCount++;
            throw new BrokerUnreachableException(
                new InvalidOperationException("simulated stub"));
        }

        PublishCallCount++;
        LastPublishedProperties = basicProperties as BasicProperties;
        LastPublishedBody = body;
        return ValueTask.CompletedTask;
    }

    public void Reset()
    {
        PublishCallCount = 0;
        LastPublishedProperties = null;
        LastPublishedBody = default;
        _currentCall = 0;
    }

    // ── 以下为 IChannel 其余成员（throw NotImplemented）────

    public Task ExchangeDeclareAsync(string exchange, string type, bool durable = false,
        bool autoDelete = false, IDictionary<string, object?>? arguments = null,
        bool passive = false, bool noWait = false,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task QueueDeclareAsync(string queue, bool durable, bool exclusive, bool autoDelete,
        IDictionary<string, object?>? arguments = null, bool passive = false, bool noWait = false,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task QueueBindAsync(string queue, string exchange, string routingKey,
        IDictionary<string, object?>? arguments = null, bool noWait = false,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<QueueDeclareOk> QueueDeclarePassiveAsync(string queue,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new QueueDeclareOk("test-queue", 0, 0));

    public Task<uint> QueuePurgeAsync(string queue, CancellationToken cancellationToken = default)
        => Task.FromResult((uint)0);

    public Task QueueDeleteAsync(string queue, bool ifUnused = false, bool ifEmpty = false,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<uint> MessageCountAsync(string queue, CancellationToken cancellationToken = default)
        => Task.FromResult((uint)0);

    public Task<ulong> ConsumerCountAsync(string queue, CancellationToken cancellationToken = default)
        => Task.FromResult((ulong)0);

    public IBasicConsumer DefaultConsumer { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public Task BasicQosAsync(uint prefetchSize, ushort prefetchCount, bool global,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public ValueTask BasicAckAsync(ulong deliveryTag, bool multiple,
        CancellationToken cancellationToken = default)
        => ValueTask.CompletedTask;

    public ValueTask BasicNackAsync(ulong deliveryTag, bool multiple, bool requeue,
        CancellationToken cancellationToken = default)
        => ValueTask.CompletedTask;

    public ValueTask BasicRejectAsync(ulong deliveryTag, bool requeue,
        CancellationToken cancellationToken = default)
        => ValueTask.CompletedTask;

    public Task BasicCancelAsync(string consumerTag, bool noWait = false,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task BasicCancelAsync(IBasicConsumer consumer, bool noWait = false,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<BasicGetResult?> BasicGetAsync(string queue, bool autoAck,
        CancellationToken cancellationToken = default)
        => Task.FromResult<BasicGetResult?>(null);

    public Task<uint> BasicRecoverAsync(bool requeue,
        CancellationToken cancellationToken = default)
        => Task.FromResult((uint)0);

    public Task<string> BasicConsumeAsync(string queue, bool autoAck,
        IAsyncBasicConsumer consumer, string consumerTag = "", bool noLocal = false,
        bool exclusive = false, IDictionary<string, object?>? arguments = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult("stub-consumer-tag");

    public Task<string> BasicConsumeAsync(string queue, bool autoAck,
        string consumerTag, bool noLocal, bool exclusive,
        IDictionary<string, object?>? arguments, IAsyncBasicConsumer consumer,
        CancellationToken cancellationToken = default)
        => Task.FromResult("stub-consumer-tag");

    public ulong NextPublishSeqNo => 1;

    public void HandleBasicReturn(AsyncEventHandler<BasicReturnEventArgs> handler) { }
    public void HandleChannelRecoveringOk(AsyncEventHandler<ChannelRecoveringOkEventArgs> handler) { }
    public void RemoveBasicReturnHandler(AsyncEventHandler<BasicReturnEventArgs> handler) { }
    public void RemoveChannelRecoveringOkHandler(AsyncEventHandler<ChannelRecoveringOkEventArgs> handler) { }

    public async Task ExchangeBindAsync(string destination, string source, string routingKey,
        IDictionary<string, object?>? arguments = null, bool noWait = false,
        CancellationToken cancellationToken = default)
        => await Task.CompletedTask;

    public async Task ExchangeUnbindAsync(string destination, string source, string routingKey,
        IDictionary<string, object?>? arguments = null, bool noWait = false,
        CancellationToken cancellationToken = default)
        => await Task.CompletedTask;

    public async Task ExchangeDeleteAsync(string exchange, bool ifUnused = false,
        bool noWait = false, CancellationToken cancellationToken = default)
        => await Task.CompletedTask;

    public async Task<ExchangeDeclareOk?> ExchangeDeclarePassiveAsync(string exchange,
        bool noWait = false, CancellationToken cancellationToken = default)
        => await Task.FromResult<ExchangeDeclareOk?>(null);

    public async Task QueueUnbindAsync(string queue, string exchange, string routingKey,
        IDictionary<string, object?>? arguments = null, CancellationToken cancellationToken = default)
        => await Task.CompletedTask;

    public async Task TxSelectAsync(CancellationToken cancellationToken = default) => await Task.CompletedTask;
    public async Task TxCommitAsync(CancellationToken cancellationToken = default) => await Task.CompletedTask;
    public async Task TxRollbackAsync(CancellationToken cancellationToken = default) => await Task.CompletedTask;

    public Task ConfirmSelectAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public async Task<bool> WaitForConfirmsAsync(CancellationToken cancellationToken = default)
        => await Task.FromResult(true);

    public async Task WaitForConfirmsOrDieAsync(TimeSpan timeout,
        CancellationToken cancellationToken = default)
        => await Task.CompletedTask;

    public async Task WaitForConfirmsOrDieAsync(CancellationToken cancellationToken = default)
        => await Task.CompletedTask;

    public event AsyncEventHandler<BasicAckEventArgs>? BasicAcksAsync { add { } remove { } }
    public event AsyncEventHandler<BasicNackEventArgs>? BasicNacksAsync { add { } remove { } }
    public event AsyncEventHandler<BasicReturnEventArgs>? BasicReturnAsync { add { } remove { } }
    public event AsyncEventHandler<CallbackExceptionEventArgs>? CallbackExceptionAsync { add { } remove { } }
    public event AsyncEventHandler<FlowControlEventArgs>? FlowControlAsync { add { } remove { } }
    public event AsyncEventHandler<ShutdownEventArgs>? ChannelShutdownAsync { add { } remove { } }
    public event AsyncEventHandler<ChannelRecoveringOkEventArgs>? ChannelRecoveringOkAsync { add { } remove { } }

    public int ChannelNumber => 1;
    public ShutdownEventArgs? CloseReason => null;
    public IAutorecoveringConnection? Connection => null;
    public bool IsClosed => false;
    public bool IsOpen => true;
    public TimeSpan ContinuationTimeout { get; set; }
    public TimeSpan HandshakeContinuationTimeout { get; set; }
    public TimeSpan RequestedHeartbeat { get; set; }

    public void Dispose() { }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    public Task OpenAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task CloseAsync(ShutdownEventArgs reason, bool abort = false, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task CloseAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public ValueTask AbortAsync(ShutdownEventArgs reason, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    public ValueTask AbortAsync(CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
}

// ════════════════════════════════════════════════════════════════
//  Tests — 完整发布流程验证
// ════════════════════════════════════════════════════════════════

public class RabbitMqEventBusTests
{
    // ── 1. IntegrationEvent 自动生成 Id 和 CreationDate ──────────
    [Fact]
    public void IntegrationEvent_AutoGenerates_Id_And_CreationDate()
    {
        var evt = new OrderCreatedEvent
        {
            OrderId = Guid.NewGuid(),
            Amount = 99.99m,
            CustomerName = "Alice"
        };

        Assert.NotEqual(Guid.Empty, evt.Id);
        Assert.True(evt.CreationDate <= DateTime.UtcNow);
        Assert.True(evt.CreationDate > DateTime.UtcNow.AddSeconds(-5));
    }

    [Fact]
    public void IntegrationEvent_PositionalRecord_Inherits_Base()
    {
        var evt = new UserRegisteredEvent(
            Guid.NewGuid(), "Bob", "bob@test.com", DateTime.UtcNow);

        Assert.NotEqual(Guid.Empty, evt.Id);
        Assert.True(evt.CreationDate <= DateTime.UtcNow);
    }

    // ── 2. 序列化/反序列化 往返测试 ──────────────────────────────
    [Fact]
    public void Serialize_Deserialize_Roundtrip_Success()
    {
        var original = new OrderCreatedEvent
        {
            OrderId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Amount = 199.99m,
            CustomerName = "Charlie"
        };

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var json = JsonSerializer.Serialize(original, original.GetType(), options);
        var restored = JsonSerializer.Deserialize<OrderCreatedEvent>(json, options)!;

        Assert.Equal(original.Id, restored.Id);
        Assert.Equal(original.CreationDate, restored.CreationDate);
        Assert.Equal(original.OrderId, restored.OrderId);
        Assert.Equal(original.Amount, restored.Amount);
        Assert.Equal(original.CustomerName, restored.CustomerName);
    }

    [Fact]
    public void Serialize_AsIntegrationEvent_Deserialize_ToConcreteType()
    {
        var original = new UserRegisteredEvent(
            Guid.NewGuid(), "Diana", "diana@test.com", DateTime.UtcNow);

        var serializeOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var json = JsonSerializer.Serialize((IntegrationEvent)original, original.GetType(), serializeOptions);
        var restored = JsonSerializer.Deserialize(json, typeof(UserRegisteredEvent), serializeOptions)
            as UserRegisteredEvent;

        Assert.NotNull(restored);
        Assert.Equal(original.UserName, restored!.UserName);
        Assert.Equal(original.Email, restored.Email);
    }

    // ── 3. DI 注册：自动扫描 Handler ─────────────────────────────
    [Fact]
    public void AddEventBus_AutoScans_IIntegrationEventHandler_Implementations()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConnectionFactory>(
            Mock.Of<IConnectionFactory>());

        // 用显式注册模拟自动扫描
        services.AddEventBus("test_queue",
            HandlerRegistrationHelper.GetMockRegistrations());

        var sp = services.BuildServiceProvider();

        // 验证 IEventBus 已注册
        var eventBus = sp.GetService<IEventBus>();
        Assert.NotNull(eventBus);
        Assert.IsType<RabbitMqEventBus>(eventBus);

        // 验证 IHostedService 也已注册（同实例）
        var hostedService = sp.GetService<IHostedService>();
        Assert.NotNull(hostedService);
        Assert.Same(eventBus, hostedService);
    }

    // ── 4. KeyedService：多 Handler 解析 ─────────────────────────
    [Fact]
    public void KeyedService_Resolves_Multiple_Handlers_For_Same_EventType()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConnectionFactory>(
            Mock.Of<IConnectionFactory>());

        var registrations = new List<HandlerRegistration>
        {
            new(typeof(OrderCreatedEvent), typeof(OrderCreatedHandler)),
            new(typeof(OrderCreatedEvent), typeof(OrderAuditLogHandler)),
        };
        services.AddEventBus("multi_handler_queue", registrations);

        var sp = services.BuildServiceProvider();

        // 两种 Handler 都应该能够通过 KeyedService 解析
        var handlers = sp.GetKeyedServices<IIntegrationEventHandler>(
            typeof(OrderCreatedEvent));
        Assert.Equal(2, handlers.Count());
    }

    // ── 5. EventBusSubscriptionInfo 类型映射 ─────────────────────
    [Fact]
    public void EventBusSubscriptionInfo_Maps_EventName_To_Type()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConnectionFactory>(
            Mock.Of<IConnectionFactory>());

        // EvenBusName("UserRegistered") 应映射到 UserRegisteredEvent 类型
        var registrations = new List<HandlerRegistration>
        {
            new(typeof(UserRegisteredEvent), typeof(UserRegisteredHandler)),
        };
        services.AddEventBus("test_queue", registrations);

        var sp = services.BuildServiceProvider();
        var subInfo = sp.GetRequiredService<EventBusSubscriptionInfo>();

        Assert.True(subInfo.EventTypes.ContainsKey("UserRegistered"));
        Assert.Equal(typeof(UserRegisteredEvent), subInfo.EventTypes["UserRegistered"]);
    }

    // ── 6. 完整发布流程（Mock RabbitMQ） ─────────────────────────
    [Fact]
    public async Task PublishAsync_Serializes_And_Calls_BasicPublish()
    {
        // Arrange: Mock IChannel → IConnectionFactory → IConnection
        var channelMock = new Mock<IChannel>();

        // ExchangeDeclareAsync 必须 mock，否则调用时 NRE
        channelMock
            .Setup(c => c.ExchangeDeclareAsync(
                It.IsAny<string>(), It.IsAny<string>(), true, false, null, false, false,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        BasicProperties? capturedProps = null;
        ReadOnlyMemory<byte> capturedBody = default;

        channelMock
            .Setup(c => c.BasicPublishAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                true,
                It.IsAny<BasicProperties>(),
                It.IsAny<ReadOnlyMemory<byte>>(),
                It.IsAny<CancellationToken>()))
            .Callback(new InvocationAction(invocation =>
            {
                capturedProps = invocation.Arguments[3] as BasicProperties;
                capturedBody = (ReadOnlyMemory<byte>)invocation.Arguments[4];
            }))
            .Returns(ValueTask.CompletedTask);

        var connectionMock = new Mock<IConnection>();
        connectionMock
            .Setup(c => c.CreateChannelAsync(
                It.IsAny<CreateChannelOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(channelMock.Object);

        var factoryMock = new Mock<IConnectionFactory>();
        factoryMock
            .Setup(f => f.CreateConnectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(connectionMock.Object);

        // 使用 FakeRabbitMqConnection 直接返回预设的 IChannel
        var fakeConnection = new FakeRabbitMqConnection(
            Mock.Of<IConnectionFactory>(), channelMock.Object);

        var options = Options.Create(new EventBusOptions
        {
            SubscriptionClientName = "publish_test_queue",
            RetryCount = 3
        });
        var subInfo = Options.Create(new EventBusSubscriptionInfo());
        var rabbitOptions = Options.Create(new IntegrationEventRabbitMqOptions());
        var telemetry = new RabbitMQTelemetry();
        var sp = new ServiceCollection().AddLogging().BuildServiceProvider();
        var logger = sp.GetRequiredService<ILogger<RabbitMqEventBus>>();

        var eventBus = new RabbitMqEventBus(
            fakeConnection, options, subInfo, rabbitOptions,
            telemetry, sp, logger);

        var testEvent = new OrderCreatedEvent
        {
            OrderId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Amount = 599.99m,
            CustomerName = "Eve"
        };

        // Act
        await eventBus.PublishAsync(testEvent);

        // Assert
        Assert.NotNull(capturedProps);
        Assert.Equal(DeliveryModes.Persistent, capturedProps!.DeliveryMode);

        var json = Encoding.UTF8.GetString(capturedBody.Span);
        Assert.Contains("22222222-2222-2222-2222-222222222222", json);
        Assert.Contains("Eve", json);
        Assert.Contains("599.99", json);

        channelMock.Verify(
            c => c.BasicPublishAsync(
                "notcomd_event_bus",
                "OrderCreatedEvent",
                true,
                It.IsAny<BasicProperties>(),
                It.IsAny<ReadOnlyMemory<byte>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── 7. Polly 重试策略配置验证 ───────────────────────────────
    [Fact]
    public async Task PublishPipeline_Uses_Polly_RetryPolicy()
    {
        var callCount = 0;
        var channelMock = new Mock<IChannel>();

        channelMock
            .Setup(c => c.ExchangeDeclareAsync(
                It.IsAny<string>(), It.IsAny<string>(), true, false, null, false, false,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        channelMock
            .Setup(c => c.BasicPublishAsync(
                It.IsAny<string>(), It.IsAny<string>(), true,
                It.IsAny<BasicProperties>(),
                It.IsAny<ReadOnlyMemory<byte>>(),
                It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                callCount++;
                if (callCount <= 2)
                    throw new BrokerUnreachableException(
                        new InvalidOperationException("simulated"));
                return ValueTask.CompletedTask;
            });

        var fakeConnection = new FakeRabbitMqConnection(
            Mock.Of<IConnectionFactory>(), channelMock.Object);

        var options = Options.Create(new EventBusOptions
        {
            SubscriptionClientName = "retry_test_queue",
            RetryCount = 5
        });
        var subInfo = Options.Create(new EventBusSubscriptionInfo());
        var rabbitOptions = Options.Create(new IntegrationEventRabbitMqOptions());
        var telemetry = new RabbitMQTelemetry();
        var sp = new ServiceCollection().AddLogging().BuildServiceProvider();
        var logger = sp.GetRequiredService<ILogger<RabbitMqEventBus>>();

        var eventBus = new RabbitMqEventBus(
            fakeConnection, options, subInfo, rabbitOptions,
            telemetry, sp, logger);

        var testEvent = new OrderCreatedEvent
        {
            OrderId = Guid.NewGuid(),
            Amount = 100m,
            CustomerName = "RetryTest"
        };

        // Act: 应该自动重试，最终成功
        await eventBus.PublishAsync(testEvent);

        // Assert: 第 1、2 次失败，第 3 次成功
        Assert.Equal(3, callCount);
    }

    // ── 8. IEventBus 接口仅包含 PublishAsync ────────────────────
    [Fact]
    public void IEventBus_OnlyHas_PublishAsync()
    {
        var methods = typeof(IEventBus).GetMethods(BindingFlags.Public | BindingFlags.Instance);
        var methodNames = methods.Select(m => m.Name).ToArray();

        Assert.Contains("PublishAsync", methodNames);
        Assert.DoesNotContain("Publish", methodNames);    // 旧 API 已移除
        Assert.DoesNotContain("Subscribe", methodNames);  // 已移除
        Assert.DoesNotContain("Unsubscribe", methodNames); // 已移除
    }

    // ── 9. EvenBusNameAttribute 自定义路由名 ────────────────────
    [Fact]
    public void EvenBusNameAttribute_Sets_Custom_RoutingKey()
    {
        var attr = typeof(UserRegisteredHandler)
            .GetCustomAttribute<EvenBusNameAttribute>();
        Assert.NotNull(attr);
        Assert.Equal("UserRegistered", attr.EventName);
    }

    // ── 10. IntegrationEventRabbitMqOptions 默认值 ──────────────
    [Fact]
    public void IntegrationEventRabbitMqOptions_Has_Correct_Defaults()
    {
        var options = new IntegrationEventRabbitMqOptions();

        Assert.Equal("notcomd_event_bus", options.ExchangeName);
        Assert.Equal(Notcomd.Evenbus.EventBus.ExchangeType.Direct, options.ExchangeType);
        Assert.Equal(".dlq", options.DeadLetterQueueSuffix);
        Assert.Equal(30, options.RequestTimeoutSeconds);
    }
}

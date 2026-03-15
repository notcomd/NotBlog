using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Notcomd.Evenbus;

public class RabbitMqEventButs : IEventBus, IDisposable
{
    private readonly IModel _consumerChannel;

    private readonly string _exchangeName;

    private readonly RabbitMqConnection _rabbitMqConnection;

    private readonly IServiceProvider _serviceProvider;

    private readonly IServiceScope _serviceScope;

    private readonly SubscriptionsManager _subscriptionsManager;

    private string _queueName;


    public RabbitMqEventButs(RabbitMqConnection rabbitMqConnection, string exchangeName, string queueName,
        IServiceScopeFactory serviceScopeFactory)
    {
        _rabbitMqConnection = rabbitMqConnection ?? throw new ArgumentNullException(nameof(rabbitMqConnection));
        _subscriptionsManager = new SubscriptionsManager();
        _exchangeName = exchangeName;
        _queueName = queueName;


        _serviceScope = serviceScopeFactory.CreateScope() ??
                        throw new ArgumentNullException($"无法创建{serviceScopeFactory.CreateScope()}");
        _serviceProvider = _serviceScope.ServiceProvider;
        _consumerChannel = CreateConsumerChannel() ?? throw new ArgumentNullException(nameof(rabbitMqConnection));
        _subscriptionsManager.OnEventRemoved += SubsManager_OnEventRemoved;
    }

    public void Dispose()
    {
        _consumerChannel.Dispose();
        _subscriptionsManager.Clear();
        _rabbitMqConnection.Dispose();
        _serviceScope.Dispose();
    }

    public void Publish(string eventName, object? eventData)
    {
        if (!_rabbitMqConnection.Isconnected) _rabbitMqConnection.TryConnect();

        using var channel = _rabbitMqConnection.CreateModel();
        channel.ExchangeDeclare(_exchangeName, ExchangeType.Direct);
        byte[] body;
        if (eventData == null)
        {
            body = [];
        }
        else
        {
            var jsonSerializerOptions = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            body = JsonSerializer.SerializeToUtf8Bytes(eventData, eventData.GetType(), jsonSerializerOptions);
        }

        var properties = channel.CreateBasicProperties();
        properties.DeliveryMode = 2;
        channel.BasicPublish(_exchangeName, eventName, true, properties, body);
    }


    public void Subscribe(string eventName, Type handlerType)
    {
        CheckHandlerType(handlerType);
        DoInternalSubscription(eventName);
        _subscriptionsManager.AddSubscription(eventName, handlerType);
        StartBasic();
    }

    public void Unsubscribe(string eventName, Type handlerType)
    {
        CheckHandlerType(handlerType);
        _subscriptionsManager.RemoveSubscription(eventName, handlerType);
    }

    private void SubsManager_OnEventRemoved(object? sender, string e)
    {
        if (!_rabbitMqConnection.Isconnected) _rabbitMqConnection.TryConnect();
        using var channel = _rabbitMqConnection.CreateModel();
        channel.QueueUnbind(_queueName, _exchangeName, e);
        if (_subscriptionsManager.IsEmpty)
        {
            _queueName = string.Empty;
            _consumerChannel.Close();
        }
    }


    
    /// <summary>
    /// 创建消费者通道
    /// </summary>
    /// <returns></returns>
    private IModel? CreateConsumerChannel()
    {
        if (!_rabbitMqConnection.Isconnected) _rabbitMqConnection.TryConnect();
        var channel = _rabbitMqConnection.CreateModel();
        channel.ExchangeDeclare(_exchangeName, ExchangeType.Direct);
        channel.QueueDeclare(_queueName, true, false, false, null);
        channel.CallbackException += (sender, ea) => { Debug.Fail(ea.ToString()); };
        return channel;
    }

    /// <summary>
    /// 启动消费者
    /// </summary>
    private void StartBasic()
    {
        if (_consumerChannel is null) return;
        var consumer = new AsyncEventingBasicConsumer(_consumerChannel);
        consumer.Received += Consumer_Received;
        _consumerChannel.BasicConsume(_queueName, false, consumer);
    }

    /// <summary>
    /// 消费者接收消息
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="event"></param>
    private async Task Consumer_Received(object sender, BasicDeliverEventArgs @event)
    {
        var eventName = @event.RoutingKey;
        var message = Encoding.UTF8.GetString(@event.Body.Span);
        try
        {
            await ProcessEvent(eventName, message);
            _consumerChannel.BasicAck(@event.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            Debug.Fail(ex.ToString());
        }
    }

    /// <summary>
    /// 处理事件
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="message"></param>
    /// <exception cref="ApplicationException"></exception>
    private async Task ProcessEvent(string eventName, string message)
    {
        if (_subscriptionsManager.HasSubscriptionForEvent(eventName))
        {
            var sub = _subscriptionsManager.GetHandlersForEvent(eventName);
            foreach (var subint in sub)
            {
                using var scope = _serviceProvider.CreateScope();
                var handler = scope.ServiceProvider.GetService(subint) as IIntegrationEventHandler;
                if (handler == null) throw new ApplicationException($"无法创建{subint}类型的服务");
                await handler.Handler(eventName, message);
            }
        }
        else
        {
            var entryAsm = Assembly.GetEntryAssembly()!.GetName().Name;
            Debug.WriteLine($"找不到可以处理evenName={eventName}的处理程序，entryAsmc:{entryAsm}");
        }
    }

    /// <summary>
    /// 订阅内部事件
    /// </summary>
    /// <param name="eventName"></param>
    private void DoInternalSubscription(string eventName)
    {
        var cont = _subscriptionsManager.HasSubscriptionForEvent(eventName);
        if (!cont)
        {
            if (!_rabbitMqConnection.Isconnected) _rabbitMqConnection.TryConnect();
            _consumerChannel.QueueBind(_queueName, _exchangeName, eventName);
        }
    }

    /// <summary>
    /// 检查处理器类型
    /// </summary>
    /// <param name="handlerType"></param>
    /// <exception cref="ArgumentException"></exception>
    private void CheckHandlerType(Type handlerType)
    {
        if (!typeof(IIntegrationEventHandler).IsAssignableFrom(handlerType))
            throw new ArgumentException($"{handlerType} doesn't inherit from IIntegrationEventHandler",
                nameof(handlerType));
    }
}
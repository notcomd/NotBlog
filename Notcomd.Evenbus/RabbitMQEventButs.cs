using Microsoft.Extensions.DependencyInjection;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Notcomd.Evenbus
{
    public class RabbitMQEventButs : IEventBus, IDisposable
    {

        private IModel _consumerChannel;
        private readonly string _exchangeNmae;
        private string _queueName;
        private readonly RabbitMQConnection _rabbitMQConnection;
        private readonly SubscriptionsManager _subscriptionsManager;
        private readonly IServiceProvider _serviceProvider;
        private readonly IServiceScope _serviceScope;


        public RabbitMQEventButs(RabbitMQConnection rabbitMQConnection, string exechangeName, string queueName, IServiceScopeFactory serviceScopeFactory)
        {
            _rabbitMQConnection = rabbitMQConnection ?? throw new ArgumentNullException(nameof(rabbitMQConnection));
            _subscriptionsManager = new SubscriptionsManager();
            _exchangeNmae = exechangeName;
            _queueName = queueName;


            _serviceScope = serviceScopeFactory.CreateScope();
            _serviceProvider = _serviceScope.ServiceProvider;
            _consumerChannel = CreateConsumerChannel();
            _subscriptionsManager.OnEventRemoved += SubsManager_OnEventRemoved; 
        }

        private void SubsManager_OnEventRemoved(object? sender, string e)
        {
            if (!_rabbitMQConnection.Isconnected)
            {
                _rabbitMQConnection.TryConnect();
            }
            using (var channel = _rabbitMQConnection.CreateModel())
            {
                channel.QueueUnbind(queue: _queueName, exchange: _exchangeNmae, routingKey: e);
                if (_subscriptionsManager.IsEmpty)
                {
                    _queueName = string.Empty;
                    _consumerChannel.Close();
                }
            }
        }


        private IModel? CreateConsumerChannel()
        {
            if (!_rabbitMQConnection.Isconnected)
            {
                _rabbitMQConnection.TryConnect();
            }
            var channel = _rabbitMQConnection.CreateModel();
            channel.ExchangeDeclare(_exchangeNmae, ExchangeType.Direct);
            channel.QueueDeclare(_queueName, true, false, false, null);
            channel.CallbackException += (sender, ea) =>
            {
                Debug.Fail(ea.ToString());
            };
            return channel;
        }

        public void Publish(string eventName, object? eventData)
        {

            if (!_rabbitMQConnection.Isconnected)
            {
                _rabbitMQConnection.TryConnect();

            }

            using (var channel = _rabbitMQConnection.CreateModel())
            {
                channel.ExchangeDeclare(exchange: _exchangeNmae, type: ExchangeType.Direct);
                byte[] body;
                if (eventData == null)
                {
                    body = new byte[0];
                }
                else
                {
                    JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
                    {
                        WriteIndented = true
                    };
                    body = JsonSerializer.SerializeToUtf8Bytes(eventData, eventData.GetType(), jsonSerializerOptions);
                }
                var properties = channel.CreateBasicProperties();
                properties.DeliveryMode = 2;
                channel.BasicPublish(_exchangeNmae, eventName, true, properties, body);
            }
        }




        public void Subscribe(string eventName, Type handlerType)
        {
            CheckHandlerType(handlerType);
            DoInternalSubscription(eventName);
            _subscriptionsManager.AddSubscription(eventName, handlerType);
            StartBasic();
        }

        private void StartBasic()
        {
            if (_consumerChannel != null)
            {
                var consumer = new AsyncEventingBasicConsumer(_consumerChannel);
                consumer.Received += Consumer_Received;
                _consumerChannel.BasicConsume(_queueName, false, consumer);
            }
        }

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

        private async Task ProcessEvent(string eventName, string message)
        {
            if (_subscriptionsManager.HasSubscriptionForEvent(eventName))
            {
                var sub = _subscriptionsManager.GetHandlersForEvent(eventName);
                foreach (var subint in sub)
                {
                    using var scope = _serviceProvider.CreateScope();
                    var handler = scope.ServiceProvider.GetService(subint) as IIntegrationEventHandler;
                    if (handler == null)
                    {
                        throw new ApplicationException($"无法创建{subint}类型的服务");
                    }
                    await handler.Eventhander(eventName, message);
                }
            }
            else
            {
                string entryAsm = Assembly.GetEntryAssembly().GetName().Name;
                Debug.WriteLine($"找不到可以处理evenName={eventName}的处理程序，entryAsmc:{entryAsm}");
            }
        }

        private void DoInternalSubscription(string eventName)
        {
            var cont = _subscriptionsManager.HasSubscriptionForEvent(eventName);
            if (!cont)
            {
                if (!_rabbitMQConnection.Isconnected)
                {
                    _rabbitMQConnection.TryConnect();
                }
                _consumerChannel.QueueBind(_queueName, _exchangeNmae, eventName);
            }
        }

        private void CheckHandlerType(Type handlerType)
        {
            if (!typeof(IIntegrationEventHandler).IsAssignableFrom(handlerType))
            {
                throw new ArgumentException($"{handlerType} doesn't inherit from IIntegrationEventHandler", nameof(handlerType));
            }
        }

        public void Unsubscribe(string eventName, Type handlerType)
        {
            CheckHandlerType(handlerType);
            _subscriptionsManager.RemoveSubscription(eventName, handlerType);
        }

        public void Dispose()
        {
            if (_consumerChannel != null)
            {
                _consumerChannel.Dispose();
            }
            _subscriptionsManager.Clear();
            _rabbitMQConnection.Dispose();
            _serviceScope.Dispose();
        }
    }
}

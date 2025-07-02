using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Notcomd.Evenbus;

public static class ServicesCollectionExtensions
{
    public static IServiceCollection AddEventBus(this IServiceCollection services, string queueName
        , params Assembly[] assemblies)
    {
        return AddEventBus(services, queueName, assemblies.ToList());
    }

    public static IServiceCollection AddEventBus(this IServiceCollection service, string queueName,
        IEnumerable<Assembly> assemblies)
    {
        List<Type> eventHandlers = new List<Type>();
        foreach (var asm in assemblies)
        {
            var types = asm.GetTypes().Where(T => T.IsAbstract == false && T.IsAssignableTo(typeof(IIntegrationEventHandler)));
            eventHandlers.AddRange(types);
        }
        return AddEventBus(service, queueName, eventHandlers);
    }

    public static IServiceCollection AddEventBus(this IServiceCollection services, string queueName, IEnumerable<Type> eventHandler)
    {
        foreach (var type in eventHandler)
        {
            services.AddScoped(type, type);
        }
        services.AddSingleton<IEventBus>(sp =>
        {
            var optionMQ = sp.GetRequiredService<IOptions<IntegrationEventRabbitMQOptions>>().Value;
            var factoy = new ConnectionFactory
            {
                HostName = optionMQ.HostName,
                DispatchConsumersAsync = true
            };
            if (optionMQ.UserName != null)
            {
                factoy.UserName = optionMQ.UserName;
            }
            if (optionMQ.Password != null)
            {
                factoy.Password = optionMQ.Password;

            }
            var rabbitMQConnection = new RabbitMQConnection(factoy);
            var serviceScopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
            var eventbus = new RabbitMQEventButs(rabbitMQConnection, optionMQ.ExchangeName, queueName, serviceScopeFactory);

            foreach (var type in eventHandler)
            {
                var eventNameAttrs = type.GetCustomAttributes<EvenBusNameAttribute>();
                if (eventNameAttrs.Any() == false)
                {
                    throw new ApplicationException($"There shoule be at least one EventNameAttribute on {type}");
                }
                foreach (var eventNameAttr in eventNameAttrs)
                {
                    eventbus.Subscribe(eventNameAttr.GetType().Name, type);
                }
            }
            return eventbus;
        });
        return services;
    }
}
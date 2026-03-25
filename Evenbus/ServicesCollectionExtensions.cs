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
        return services.AddEventBus(queueName, assemblies.ToList());
    }

    public static IServiceCollection AddEventBus(this IServiceCollection services, string queueName,
        IEnumerable<Assembly> assemblies)
    {
        var eventHandlers = new List<Type>();
        foreach (var asm in assemblies)
        {
            var types =
                asm.GetTypes().Where(T => !T.IsAbstract && T.IsAssignableTo(typeof(IIntegrationEventHandler)));
            eventHandlers.AddRange(types);
        }

        return services.AddEventBus(queueName, eventHandlers);
    }

    public static IServiceCollection AddEventBus(this IServiceCollection services, string queueName,
        IEnumerable<Type> eventHandler)
    {
        var enumerable = eventHandler as Type[] ?? eventHandler.ToArray();
        foreach (var type in enumerable) services.AddScoped(type, type);
        services.AddSingleton<IEventBus>(sp =>
        {
            var optionMq = sp.GetRequiredService<IOptions<IntegrationEventRabbitMqOptions>>().Value;
            var factoy = new ConnectionFactory
            {
                HostName = optionMq.HostName
            };
            if (optionMq.UserName != null) factoy.UserName = optionMq.UserName;
            if (optionMq.Password != null) factoy.Password = optionMq.Password;
            var rabbitMqConnection = new RabbitMqConnection(factoy);
            var serviceScopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
            var eventbus = new RabbitMqEventButs(rabbitMqConnection, optionMq.ExchangeName, queueName,
                serviceScopeFactory);

            foreach (var type in enumerable)
            {
                var eventNameAttrs = type.GetCustomAttributes<EvenBusNameAttribute>();
                var evenBusNameAttributes = eventNameAttrs as EvenBusNameAttribute[] ?? eventNameAttrs.ToArray();
                if (!evenBusNameAttributes.Any())
                    throw new ApplicationException($"There should be at least one EventNameAttribute on {type}");
                foreach (var eventNameAttr in evenBusNameAttributes)
                    eventbus.Subscribe(eventNameAttr.GetType().Name, type);
            }

            return eventbus;
        });
        return services;
    }
}
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Notcomd.Evenbus.EventBus;
using RabbitMQ.Client;

namespace Notcomd.Evenbus;

public static class ServicesCollectionExtensions
{
    /// <summary>
    /// 注册 EventBus（自动扫描程序集中的 Handler）
    /// </summary>
    public static IServiceCollection AddEventBus(this IServiceCollection services, string queueName,
        params Assembly[] assemblies)
    {
        var eventHandlers = new List<Type>();
        foreach (var asm in assemblies)
        {
            var types = asm.GetTypes()
                .Where(t => !t.IsAbstract && t.IsAssignableTo(typeof(IIntegrationEventHandler)));
            eventHandlers.AddRange(types);
        }

        return services.AddEventBus(queueName, eventHandlers);
    }

    /// <summary>
    /// 注册 EventBus（指定 Handler 类型列表）
    /// </summary>
    public static IServiceCollection AddEventBus(this IServiceCollection services, string queueName,
        IEnumerable<Type> eventHandlers)
    {
        var handlerList = eventHandlers as Type[] ?? eventHandlers.ToArray();
        foreach (var type in handlerList) services.AddScoped(type, type);

        services.AddSingleton<IEventBus>(sp =>
        {
            var optionMq = sp.GetRequiredService<IOptions<IntegrationEventRabbitMqOptions>>().Value;
            var factory = new ConnectionFactory { HostName = optionMq.HostName };
            if (!string.IsNullOrEmpty(optionMq.UserName)) factory.UserName = optionMq.UserName;
            if (!string.IsNullOrEmpty(optionMq.Password)) factory.Password = optionMq.Password;

            var rabbitMqConnection = new RabbitMqConnection(factory);
            var serviceScopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
            var eventBus = new RabbitMqEventBus(rabbitMqConnection, optionMq, queueName,
                serviceScopeFactory);

            foreach (var type in handlerList)
            {
                var eventNameAttrs = type.GetCustomAttributes<EvenBusNameAttribute>();
                var attrs = eventNameAttrs as EvenBusNameAttribute[] ?? eventNameAttrs.ToArray();
                if (!attrs.Any())
                    throw new ApplicationException(
                        $"Handler {type.Name} must have at least one [{nameof(EvenBusNameAttribute)}]");
                foreach (var attr in attrs)
                    // 修复: 使用属性的真实 EventName 值，而非类型名
                    eventBus.Subscribe(attr.EventName, type).GetAwaiter().GetResult();
            }

            return eventBus;
        });

        return services;
    }

    /// <summary>
    /// 注册 RequestBus（RPC 同步调用）
    /// </summary>
    public static IServiceCollection AddRequestBus(this IServiceCollection services)
    {
        services.AddSingleton<IRequestBus>(sp =>
        {
            var optionMq = sp.GetRequiredService<IOptions<IntegrationEventRabbitMqOptions>>().Value;
            var factory = new ConnectionFactory { HostName = optionMq.HostName };
            if (!string.IsNullOrEmpty(optionMq.UserName)) factory.UserName = optionMq.UserName;
            if (!string.IsNullOrEmpty(optionMq.Password)) factory.Password = optionMq.Password;

            var connection = new RabbitMqConnection(factory);
            return new RabbitMqRequestBus(connection, optionMq);
        });
        return services;
    }

    /// <summary>
    /// 注册 Outbox（分布式事务消息持久化）
    /// </summary>
    public static IServiceCollection AddOutbox<TDbContext>(this IServiceCollection services,
        Action<OutboxOptions> configure) where TDbContext : DbContext
    {
        var options = new OutboxOptions();
        configure?.Invoke(options);
        services.Configure(configure);
        services.AddScoped<IOutboxStore, EfCoreOutboxStore<TDbContext>>();
        services.AddHostedService<OutboxPublisher<TDbContext>>();
        return services;
    }
}
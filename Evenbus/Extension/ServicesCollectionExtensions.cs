using System.Reflection;
using Evenbus.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Notcomd.Evenbus.Core;
using Notcomd.Evenbus.EventBus;
using RabbitMQ.Client;

namespace Notcomd.Evenbus.Extension;

public static class ServicesCollectionExtensions
{
    private const string EventBusSectionName = "EventBus";

    /// <summary>
    /// 注册 EventBus（自动扫描指定程序集中的集成事件处理器）
    /// 
    /// 使用方式:
    ///   1. 注册 IConnectionFactory:
    ///      builder.Services.AddSingleton&lt;IConnectionFactory&gt;(new ConnectionFactory { HostName = "localhost" });
    ///   2. 注册 EventBus:
    ///      builder.Services.AddEventBus("my_queue", Assembly.GetExecutingAssembly());
    /// 
    /// 不再需要手动调用 UseEventBusAsync()，消费者由 IHostedService 自动启动。
    /// </summary>
    public static IServiceCollection AddEventBus(this IServiceCollection services, string queueName,
        params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assemblies);

        // 1. 扫描程序集中的所有 IIntegrationEventHandler&lt;T&gt; 实现
        var handlerRegistrations = new List<HandlerRegistration>();
        foreach (var asm in assemblies)
        {
            foreach (var type in asm.GetTypes())
            {
                if (type.IsAbstract || type.IsInterface) continue;

                // 查找 IIntegrationEventHandler&lt;T&gt; 接口实现
                var handlerInterface = type.GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType
                        && i.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>));

                if (handlerInterface == null) continue;

                var eventType = handlerInterface.GetGenericArguments()[0];
                handlerRegistrations.Add(new HandlerRegistration(eventType, type));
            }
        }

        return services.AddEventBusInternal(queueName, handlerRegistrations);
    }

    /// <summary>
    /// 注册 EventBus（指定 EventType → HandlerType 映射列表）
    /// </summary>
    public static IServiceCollection AddEventBus(this IServiceCollection services, string queueName,
        IEnumerable<HandlerRegistration> handlers)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(handlers);

        return services.AddEventBusInternal(queueName, handlers.ToList());
    }

    private static IServiceCollection AddEventBusInternal(this IServiceCollection services,
        string queueName, List<HandlerRegistration> handlers)
    {
        // 2. 注册 EventBus 配置
        services.Configure<EventBusOptions>(options =>
        {
            options.SubscriptionClientName = queueName;
        });

        // 3. 使用 KeyedTransient 注册 Handler（一个事件类型可注册多个 Handler）
        foreach (var reg in handlers)
        {
            services.AddKeyedTransient(typeof(IIntegrationEventHandler), reg.EventType, reg.HandlerType);

            // 同时注册 Handler 为自身类型（兼容直接注入）
            services.TryAddTransient(reg.HandlerType, reg.HandlerType);

            // 检查 EvenBusNameAttribute，支持自定义路由名
            var attr = reg.HandlerType.GetCustomAttribute<EvenBusNameAttribute>();
            var eventName = attr?.EventName ?? reg.EventType.Name;

            // 记录事件类型映射（用于运行时反序列化）
            services.Configure<EventBusSubscriptionInfo>(o =>
            {
                o.EventTypes[eventName] = reg.EventType;
            });
        }

        // 4. 注册 RabbitMQ 核心组件
        services.TryAddSingleton<RabbitMqConnection>();
        services.TryAddSingleton<RabbitMQTelemetry>();

        // 5. 注册 EventBusSubscriptionInfo（Singleton，因为 EventTypes 在所有消费者间共享）
        services.TryAddSingleton(sp =>
            sp.GetRequiredService<IOptions<EventBusSubscriptionInfo>>().Value);

        // 6. 注册 RabbitMqEventBus（Singleton: IEventBus + IHostedService）
        services.AddSingleton<RabbitMqEventBus>();
        services.AddSingleton<IEventBus>(sp => sp.GetRequiredService<RabbitMqEventBus>());
        services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<RabbitMqEventBus>());

        // 7. 注册 OpenTelemetry 追踪
        services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing.AddSource(RabbitMQTelemetry.ActivitySourceName);
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
            var connection = sp.GetRequiredService<RabbitMqConnection>();
            var options = sp.GetRequiredService<IOptions<IntegrationEventRabbitMqOptions>>().Value;
            var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
            return new RabbitMqRequestBus(connection, options, scopeFactory);
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

/// <summary>
/// Handler 注册信息（EventType → HandlerType 映射）
/// </summary>
public record HandlerRegistration(Type EventType, Type HandlerType);

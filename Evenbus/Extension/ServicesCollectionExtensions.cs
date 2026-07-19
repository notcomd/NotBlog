using System.Reflection;
using Evenbus.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
    // ═══════════════════════════════════════════════════════════
    //  AddEventBus 重载（新：配置驱动 + Aspire 兼容）
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 注册 EventBus（从 IConfiguration 绑定选项，Aspire 推荐用法）
    /// 
    /// 使用方式（Aspire 集成）:
    ///   builder.AddRabbitMQClient("EventBus");
    ///   builder.Services.AddEventBus(builder.Configuration.GetSection("EventBus"), assemblies);
    ///   
    /// appsettings.json 示例:
    ///   {
    ///     "EventBus": {
    ///       "SubscriptionClientName": "identity_events",
    ///       "ExchangeName": "notcomd_event_bus",
    ///       "PrefetchCount": 10,
    ///       "MaxConcurrency": 4
    ///     }
    ///   }
    /// </summary>
    public static IServiceCollection AddEventBus(this IServiceCollection services,
        IConfiguration configuration, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // 绑定 EventBusOptions（支持热重载）
        services.Configure<EventBusOptions>(configuration);

        // 同步 IntegrationEventRabbitMqOptions（向后兼容）
        services.Configure<IntegrationEventRabbitMqOptions>(configuration);

        // 获取选项值用于注册
        var options = configuration.Get<EventBusOptions>() ?? new EventBusOptions();
        var queueName = options.SubscriptionClientName;

        return services.AddEventBusInternal(queueName, ScanHandlers(assemblies));
    }

    /// <summary>
    /// 注册 EventBus（Lambda 配置，无需 appsettings.json）
    /// </summary>
    public static IServiceCollection AddEventBus(this IServiceCollection services,
        Action<EventBusOptions> configure, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure(configure);

        // 同步 IntegrationEventRabbitMqOptions
        services.Configure<IntegrationEventRabbitMqOptions>(opt =>
        {
            var eventBusOpts = new EventBusOptions();
            configure(eventBusOpts);
            opt.ExchangeName = eventBusOpts.ExchangeName;
            opt.ExchangeType = eventBusOpts.ExchangeType;
            opt.DeadLetterQueueSuffix = eventBusOpts.DeadLetterQueueSuffix;
            opt.RequestTimeoutSeconds = eventBusOpts.RequestTimeoutSeconds;
        });

        var options = new EventBusOptions();
        configure(options);

        return services.AddEventBusInternal(options.SubscriptionClientName, ScanHandlers(assemblies));
    }

    /// <summary>
    /// 注册 EventBus（队列名 + 程序集扫描，向后兼容）
    /// </summary>
    public static IServiceCollection AddEventBus(this IServiceCollection services, string queueName,
        params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assemblies);

        return services.AddEventBusInternal(queueName, ScanHandlers(assemblies));
    }

    /// <summary>
    /// 注册 EventBus（指定 HandlerRegistration 列表，向后兼容）
    /// </summary>
    public static IServiceCollection AddEventBus(this IServiceCollection services, string queueName,
        IEnumerable<HandlerRegistration> handlers)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(handlers);

        return services.AddEventBusInternal(queueName, handlers.ToList());
    }

    // ═══════════════════════════════════════════════════════════
    //  内部实现
    // ═══════════════════════════════════════════════════════════

    private static IServiceCollection AddEventBusInternal(this IServiceCollection services,
        string queueName, List<HandlerRegistration> handlers)
    {
        // 1. 设置 EventBusOptions（如果尚未通过 IConfiguration 或 Action 配置，则设为传入的队列名）
        services.Configure<EventBusOptions>(options =>
        {
            options.SubscriptionClientName = queueName;
        });

        // 2. 使用 KeyedTransient 注册 Handler（一个事件类型可注册多个 Handler）
        foreach (var reg in handlers)
        {
            services.AddKeyedTransient(typeof(IIntegrationEventHandler), reg.EventType, reg.HandlerType);

            // 同时注册 Handler 为自身类型（兼容直接注入）
            services.TryAddTransient(reg.HandlerType, reg.HandlerType);

            // 检查 EvenBusNameAttribute，支持自定义路由名
            var attr = reg.HandlerType.GetCustomAttribute<EventBusNameAttribute>();
            var eventName = attr?.EventName ?? reg.EventType.Name;

            // 记录事件类型映射（用于运行时反序列化）
            services.Configure<EventBusSubscriptionInfo>(o =>
            {
                o.EventTypes[eventName] = reg.EventType;
            });
        }

        // 3. 注册 RabbitMQ 核心组件
        services.TryAddSingleton<RabbitMqConnection>();
        services.TryAddSingleton<RabbitMQTelemetry>();

        // 4. 注册 EventBusSubscriptionInfo（Singleton）
        services.TryAddSingleton(sp =>
            sp.GetRequiredService<IOptions<EventBusSubscriptionInfo>>().Value);

        // 5. 注册 RabbitMqEventBus（Singleton: IEventBus + IHostedService）
        services.AddSingleton<RabbitMqEventBus>();
        services.AddSingleton<IEventBus>(sp => sp.GetRequiredService<RabbitMqEventBus>());
        services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<RabbitMqEventBus>());

        // 6. 注册 OpenTelemetry 追踪
        services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing.AddSource(RabbitMQTelemetry.ActivitySourceName);
            });

        return services;
    }

    /// <summary>
    /// 扫描程序集中的 IIntegrationEventHandler&lt;T&gt; 实现
    /// </summary>
    private static List<HandlerRegistration> ScanHandlers(Assembly[] assemblies)
    {
        var handlerRegistrations = new List<HandlerRegistration>();
        foreach (var asm in assemblies)
        {
            foreach (var type in asm.GetTypes())
            {
                if (type.IsAbstract || type.IsInterface) continue;

                var handlerInterface = type.GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType
                        && i.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>));

                if (handlerInterface == null) continue;

                var eventType = handlerInterface.GetGenericArguments()[0];
                handlerRegistrations.Add(new HandlerRegistration(eventType, type));
            }
        }
        return handlerRegistrations;
    }

    // ═══════════════════════════════════════════════════════════
    //  RequestBus / Outbox（保持不变）
    // ═══════════════════════════════════════════════════════════

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

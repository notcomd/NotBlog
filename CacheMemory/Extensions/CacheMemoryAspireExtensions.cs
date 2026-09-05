using System.Collections.Concurrent;

using CacheMemory.Core;
using CacheMemory.Providers;
using CacheMemory.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CacheMemory.Extensions;

/// <summary>
/// CacheMemory 库的 Aspire 风格扩展方法。
/// 提供基于 <see cref="IHostApplicationBuilder"/> 的注册方式，
/// 兼容 Aspire 的 ConnectionStrings 配置机制和健康检查。
/// </summary>
/// <remarks>
/// <para>使用方式（Aspire 项目）：</para>
/// <code>
/// // Program.cs
/// var builder = WebApplication.CreateBuilder(args);
/// builder.AddServiceDefaults();
/// builder.AddCacheMemory("Redis");  // 自动从 ConnectionStrings:Redis 读取
/// </code>
/// </remarks>
public static class CacheMemoryAspireExtensions
{
    /// <summary>
    /// 进程内健康检查注册锁：同一宿主可能重复调用 AddCacheMemory 多次，
    /// 避免向 HealthCheckService 重复注册同名检查导致启动失败。
    /// </summary>
    private static readonly ConcurrentDictionary<string, byte> RegisteredHealthChecks = new();

    /// <summary>
    /// 幂等注册 CacheMemory 健康检查（同名检查重复注册会抛 ArgumentException）。
    /// </summary>
    internal static void TryRegisterCacheMemoryHealthCheck(IServiceCollection services)
    {
        if (RegisteredHealthChecks.TryAdd(CacheMemoryHealthCheck.Name, 0))
        {
            services.AddHealthChecks()
                .AddCheck<CacheMemoryHealthCheck>(CacheMemoryHealthCheck.Name, tags: ["redis", "cache"]);
        }
    }
    /// <summary>
    /// 注册 CacheMemory Redis 缓存服务（Aspire 风格）。
    /// 自动从 Aspire 的 ConnectionStrings 配置节读取指定名称的连接字符串。
    /// </summary>
    /// <param name="builder">Aspire 的 HostApplicationBuilder</param>
    /// <param name="connectionName">Aspire 连接名称（对应 AppHost 中的 AddRedis("name") 或 AddConnectionString("name")）</param>
    /// <param name="configure">可选的配置回调，用于覆盖或补充配置</param>
    /// <returns>builder，支持链式调用</returns>
    /// <example>
    /// <code>
    /// // 单实例
    /// builder.AddCacheMemory("Redis");
    ///
    /// // 带额外配置
    /// builder.AddCacheMemory("Redis", options =>
    /// {
    ///     options.Retry.MaxRetryCount = 5;
    /// });
    /// </code>
    /// </example>
    public static IHostApplicationBuilder AddCacheMemory(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<CacheMemoryOption>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionName);

        // 1. 构建配置：灵活 merge Aspire ConnectionStrings 和自定义配置
        var options = BuildCacheMemoryOptions(builder, connectionName, configure);

        // 2. 注册核心服务
        RegisterCoreServices(builder.Services, options);

        // 3. 幂等注册健康检查（多次调用 AddCacheMemory 不会重复注册同名检查）
        TryRegisterCacheMemoryHealthCheck(builder.Services);

        return builder;
    }

    /// <summary>
    /// 注册 CacheMemory 支持多实例 Redis（Aspire 风格）。
    /// 每个连接名称自动映射为同名的 CacheMemory 实例。
    /// </summary>
    /// <param name="builder">Aspire 的 HostApplicationBuilder</param>
    /// <param name="connectionNames">Aspire 连接名称集合</param>
    /// <param name="configure">可选的配置回调</param>
    /// <returns>builder</returns>
    /// <example>
    /// <code>
    /// // 多实例
    /// builder.AddCacheMemory("Redis", "SessionCache", "AnalyticsCache");
    /// </code>
    /// </example>
    public static IHostApplicationBuilder AddCacheMemory(
        this IHostApplicationBuilder builder,
        string[] connectionNames,
        Action<CacheMemoryOption>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(connectionNames);

        if (connectionNames.Length == 0)
            throw new ArgumentException("至少需要指定一个连接名称", nameof(connectionNames));

        // 第一个连接名称作为 "Default" 实例
        var primaryName = connectionNames[0];

        var options = new CacheMemoryOption();
        options.Instances.Clear();

        foreach (var name in connectionNames)
        {
            var connString = builder.Configuration.GetConnectionString(name);
            if (!string.IsNullOrWhiteSpace(connString))
            {
                options.Instances[name] = new RedisInstanceOptions { ConnectionString = connString };
            }
        }

        // 确保默认实例指向第一个连接
        if (options.Instances.Count > 0 && !options.Instances.ContainsKey(CacheMemoryOption.DefaultInstanceName))
        {
            options.Instances[CacheMemoryOption.DefaultInstanceName] = options.Instances[primaryName];
        }

        configure?.Invoke(options);

        RegisterCoreServices(builder.Services, options);

        TryRegisterCacheMemoryHealthCheck(builder.Services);

        return builder;
    }

    /// <summary>
    /// 注册 CacheMemory 使用自定义配置委托（Aspire 风格，不依赖 ConnectionStrings）。
    /// </summary>
    /// <param name="builder">Aspire 的 HostApplicationBuilder</param>
    /// <param name="configure">配置回调</param>
    /// <returns>builder</returns>
    public static IHostApplicationBuilder AddCacheMemory(
        this IHostApplicationBuilder builder,
        Action<CacheMemoryOption> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new CacheMemoryOption();
        configure(options);

        RegisterCoreServices(builder.Services, options);

        TryRegisterCacheMemoryHealthCheck(builder.Services);

        return builder;
    }

    /// <summary>
    /// 构建 CacheMemoryOption，优先从 Aspire ConnectionStrings 读取连接字符串。
    /// </summary>
    private static CacheMemoryOption BuildCacheMemoryOptions(
        IHostApplicationBuilder builder,
        string connectionName,
        Action<CacheMemoryOption>? configure)
    {
        var options = new CacheMemoryOption();

        // 尝试从 Aspire ConnectionStrings 读取
        var connString = builder.Configuration.GetConnectionString(connectionName);
        if (!string.IsNullOrWhiteSpace(connString))
        {
            options.Instances[CacheMemoryOption.DefaultInstanceName] = new RedisInstanceOptions
            {
                ConnectionString = connString
            };
        }

        // 也尝试从传统 CacheMemory 配置节加载（作为补充）
        var cacheSection = builder.Configuration.GetSection(CacheMemoryOption.DefaultSectionName);
        if (cacheSection.Exists())
        {
            var config = new CacheMemoryConfig(builder.Configuration);
            config.ApplyAspireConnectionString(options, connectionName);
        }

        configure?.Invoke(options);

        return options;
    }

    /// <summary>
    /// 注册 CacheMemory 所有核心服务到 DI 容器（内部共用注册逻辑）。
    /// </summary>
    internal static void RegisterCoreServices(IServiceCollection services, CacheMemoryOption options)
    {
        // 注册配置
        services.AddSingleton(options);

        // 注册重试策略
        services.TryAddSingleton<IRedisRetryPolicy>(sp =>
        {
            var opt = sp.GetRequiredService<CacheMemoryOption>();
            var logger = sp.GetService<ILogger<RedisRetryPolicy>>();
            return new RedisRetryPolicy(opt.Retry, logger);
        });

        // 注册连接提供者
        services.TryAddSingleton<IRedisConnectionProvider, RedisConnectionProvider>();

        // 注册核心缓存服务
        services.TryAddSingleton<IRedisCacheService, RedisCacheService>();

        // 注册分布式锁服务
        services.TryAddSingleton<RedisDistributedLock>();

        // 注册发布订阅服务
        services.TryAddSingleton<RedisPubSubService>();

        // 注册泛型缓存服务
        services.TryAddSingleton(typeof(ICacheMemory<>), typeof(CacheMemory<>));

        // 注册健康检查
        services.TryAddSingleton<CacheMemoryHealthCheck>();
    }
}
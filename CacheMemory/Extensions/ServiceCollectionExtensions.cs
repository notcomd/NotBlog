using CacheMemory.Core;
using CacheMemory.Providers;
using CacheMemory.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace CacheMemory.Extensions;

/// <summary>
/// CacheMemory 库的 DI 注册扩展方法。
/// 提供便捷的 IServiceCollection 扩展，自动注册所有必需的 Redis 服务。
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 注册 CacheMemory Redis 缓存服务。
    /// 从 <see cref="IConfiguration"/> 的 "CacheMemory" 节读取配置。
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    /// <returns>服务集合，支持链式调用</returns>
    public static IServiceCollection AddCacheMemory(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // 1. 加载配置
        var config = new CacheMemoryConfig(configuration);
        var options = config.GetOptions();

        // 2. 注册配置为单例
        services.AddSingleton(options);

        // 3. 注册重试策略（每个实例独立，但此处注册默认）
        services.TryAddSingleton<IRedisRetryPolicy>(sp =>
        {
            var opt = sp.GetRequiredService<CacheMemoryOption>();
            var logger = sp.GetService<ILogger<RedisRetryPolicy>>();
            return new RedisRetryPolicy(opt.Retry, logger);
        });

        // 4. 注册连接提供者
        services.TryAddSingleton<IRedisConnectionProvider, RedisConnectionProvider>();

        // 5. 注册核心缓存服务
        services.TryAddSingleton<IRedisCacheService, RedisCacheService>();

        // 6. 注册分布式锁服务
        services.TryAddSingleton<RedisDistributedLock>();

        // 7. 注册发布订阅服务
        services.TryAddSingleton<RedisPubSubService>();

        // 8. 注册泛型缓存服务（开放式泛型）
        services.TryAddSingleton(typeof(ICacheMemory<>), typeof(CacheMemory<>));

        return services;
    }

    /// <summary>
    /// 注册 CacheMemory Redis 缓存服务，使用自定义配置委托。
    /// 适用于不需要 IConfiguration 的场景。
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configure">配置委托</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddCacheMemory(
        this IServiceCollection services,
        Action<CacheMemoryOption> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new CacheMemoryOption();
        configure(options);

        // 应用环境变量覆盖
        ApplyEnvironmentOverrides(options);

        services.AddSingleton(options);

        services.TryAddSingleton<IRedisRetryPolicy>(sp =>
        {
            var opt = sp.GetRequiredService<CacheMemoryOption>();
            var logger = sp.GetService<ILogger<RedisRetryPolicy>>();
            return new RedisRetryPolicy(opt.Retry, logger);
        });

        services.TryAddSingleton<IRedisConnectionProvider, RedisConnectionProvider>();
        services.TryAddSingleton<IRedisCacheService, RedisCacheService>();
        services.TryAddSingleton<RedisDistributedLock>();
        services.TryAddSingleton<RedisPubSubService>();
        services.TryAddSingleton(typeof(ICacheMemory<>), typeof(CacheMemory<>));

        return services;
    }

    /// <summary>
    /// 注册 CacheMemory 并预热所有实例连接（推荐在生产环境使用）。
    /// 在应用启动时建立所有 Redis 连接，避免首次请求的延迟。
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    /// <returns>服务集合</returns>
    public static async Task<IServiceCollection> AddCacheMemoryWithWarmupAsync(
        this IServiceCollection services,
        IConfiguration configuration,
        CancellationToken ct = default)
    {
        AddCacheMemory(services, configuration);

        // 构建临时服务提供者以预热连接
        var sp = services.BuildServiceProvider();
        var provider = sp.GetRequiredService<RedisConnectionProvider>();
        await provider.InitializeAllAsync(ct).ConfigureAwait(false);

        return services;
    }

    /// <summary>
    /// 应用环境变量覆盖配置。
    /// </summary>
    private static void ApplyEnvironmentOverrides(CacheMemoryOption options)
    {
        const string prefix = "CacheMemory__Instances__";

        foreach (var (instanceName, instance) in options.Instances)
        {
            var connEnv = Environment.GetEnvironmentVariable($"{prefix}{instanceName}__ConnectionString");
            if (!string.IsNullOrWhiteSpace(connEnv))
                instance.ConnectionString = connEnv;
        }

        var globalConnEnv = Environment.GetEnvironmentVariable("CacheMemory__ConnectionString");
        if (!string.IsNullOrWhiteSpace(globalConnEnv) &&
            options.Instances.TryGetValue(CacheMemoryOption.DefaultInstanceName, out var defaultInstance))
        {
            defaultInstance.ConnectionString = globalConnEnv;
        }
    }
}
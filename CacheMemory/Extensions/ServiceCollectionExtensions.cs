using CacheMemory.Core;
using CacheMemory.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CacheMemory.Extensions;

/// <summary>
/// CacheMemory 库的 DI 注册扩展方法。
/// 提供便捷的 IServiceCollection 扩展，自动注册所有必需的 Redis 服务。
/// 同时兼容传统 appsettings.json 配置和 Aspire ConnectionStrings 配置。
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 注册 CacheMemory Redis 缓存服务（从 IConfiguration 的 "CacheMemory" 节读取配置）。
    /// 同时也检查 Aspire 的 ConnectionStrings:CachMemory 作为默认实例的连接字符串补充。
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

        // 尝试从 Aspire ConnectionStrings 补充连接（兼容 Aspire 环境）
        var aspireDefaultConn = configuration.GetConnectionString("CacheMemory");
        if (!string.IsNullOrWhiteSpace(aspireDefaultConn))
        {
            options.Instances[CacheMemoryOption.DefaultInstanceName] = new RedisInstanceOptions
            {
                ConnectionString = aspireDefaultConn
            };
        }

        // 注册核心服务
        CacheMemoryAspireExtensions.RegisterCoreServices(services, options);

        // 注册健康检查
        services.AddHealthChecks()
            .AddCheck<CacheMemoryHealthCheck>(CacheMemoryHealthCheck.Name, tags: ["redis", "cache"]);

        return services;
    }

    /// <summary>
    /// 注册 CacheMemory Redis 缓存服务（Aspire 兼容模式）。
    /// 同时从 CacheMemory 配置节和 Aspire ConnectionStrings 读取连接字符串。
    /// Aspire 连接字符串优先级更高（会覆盖传统配置中的 Default 实例连接）。
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    /// <param name="connectionName">Aspire 连接名称（对应 ConnectionStrings:{connectionName}）</param>
    /// <returns>服务集合</returns>
    /// <example>
    /// <code>
    /// // 在 Aspire 项目中，Redis 由 AppHost 的 AddRedis("Redis") 提供
    /// services.AddCacheMemory(builder.Configuration, "Redis");
    /// </code>
    /// </example>
    public static IServiceCollection AddCacheMemory(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionName);

        // 先加载传统配置
        var config = new CacheMemoryConfig(configuration);
        var options = config.GetOptions();

        // Aspire 连接字符串覆盖 Default 实例（优先级更高）
        config.ApplyAspireConnectionString(options, connectionName);

        CacheMemoryAspireExtensions.RegisterCoreServices(services, options);

        services.AddHealthChecks()
            .AddCheck<CacheMemoryHealthCheck>(CacheMemoryHealthCheck.Name, tags: ["redis", "cache"]);

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

        CacheMemoryAspireExtensions.RegisterCoreServices(services, options);

        services.AddHealthChecks()
            .AddCheck<CacheMemoryHealthCheck>(CacheMemoryHealthCheck.Name, tags: ["redis", "cache"]);

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
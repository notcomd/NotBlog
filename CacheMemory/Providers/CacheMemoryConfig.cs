using CacheMemory.Core;
using Microsoft.Extensions.Configuration;

namespace CacheMemory.Providers;

/// <summary>
/// CacheMemory 配置加载器。
/// 负责从 <see cref="IConfiguration"/> 和环境变量中读取并构建完整的 <see cref="CacheMemoryOption"/>。
/// 支持多 Redis 实例配置，每个实例的连接字符串可通过环境变量覆盖。
/// </summary>
/// <remarks>
/// 配置加载优先级（从高到低）：
/// 1. 环境变量：CacheMemory__Instances__{Name}__ConnectionString
/// 2. 环境变量：CacheMemory__Instances__{Name}__DefaultDatabase
/// 3. appsettings.json 或其他 IConfiguration 源
/// 4. 代码默认值
/// </remarks>
public class CacheMemoryConfig
{
    private readonly IConfiguration _configuration;

    public CacheMemoryConfig(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// 获取完整的 CacheMemory 配置选项。
    /// </summary>
    public CacheMemoryOption GetOptions()
    {
        var section = _configuration.GetSection(CacheMemoryOption.DefaultSectionName);
        var options = new CacheMemoryOption();
        section.Bind(options);

        // 加载重试配置
        var retrySection = section.GetSection("Retry");
        if (retrySection.Exists())
            retrySection.Bind(options.Retry);

        // 加载多实例配置
        var instancesSection = section.GetSection("Instances");
        if (instancesSection.Exists())
        {
            options.Instances.Clear();
            foreach (var child in instancesSection.GetChildren())
            {
                var instance = new RedisInstanceOptions();
                child.Bind(instance);
                options.Instances[child.Key] = instance;
            }
        }

        // 确保至少有一个默认实例
        if (options.Instances.Count == 0)
            options.Instances[CacheMemoryOption.DefaultInstanceName] = new RedisInstanceOptions();

        // 应用环境变量覆盖
        ApplyEnvironmentOverrides(options);

        return options;
    }

    /// <summary>
    /// 从环境变量覆盖连接字符串等敏感配置。
    /// 环境变量命名规则：CacheMemory__Instances__{Name}__ConnectionString
    /// </summary>
    private static void ApplyEnvironmentOverrides(CacheMemoryOption options)
    {
        const string prefix = "CacheMemory__Instances__";

        foreach (var (instanceName, instance) in options.Instances)
        {
            // 覆盖连接字符串
            var connEnv = Environment.GetEnvironmentVariable($"{prefix}{instanceName}__ConnectionString");
            if (!string.IsNullOrWhiteSpace(connEnv))
                instance.ConnectionString = connEnv;

            // 覆盖默认数据库
            var dbEnv = Environment.GetEnvironmentVariable($"{prefix}{instanceName}__DefaultDatabase");
            if (int.TryParse(dbEnv, out var db))
                instance.DefaultDatabase = db;

            // 覆盖连接超时
            var timeoutEnv = Environment.GetEnvironmentVariable($"{prefix}{instanceName}__ConnectTimeoutMs");
            if (int.TryParse(timeoutEnv, out var timeout))
                instance.ConnectTimeoutMs = timeout;
        }

        // 全局环境变量：CacheMemory__ConnectionString（简化单实例场景）
        var globalConnEnv = Environment.GetEnvironmentVariable("CacheMemory__ConnectionString");
        if (!string.IsNullOrWhiteSpace(globalConnEnv) &&
            options.Instances.TryGetValue(CacheMemoryOption.DefaultInstanceName, out var defaultInstance))
        {
            defaultInstance.ConnectionString = globalConnEnv;
        }
    }
}
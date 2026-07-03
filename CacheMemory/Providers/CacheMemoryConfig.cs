using CacheMemory.Core;
using Microsoft.Extensions.Configuration;

namespace CacheMemory.Providers;

/// <summary>
/// CacheMemory 配置加载器。
/// 负责从 <see cref="IConfiguration"/>、Aspire ConnectionStrings 和环境变量中读取并构建完整的 <see cref="CacheMemoryOption"/>。
/// 支持多 Redis 实例配置，每个实例的连接字符串可通过环境变量覆盖。
/// </summary>
/// <remarks>
/// 配置加载优先级（从高到低）：
/// 1. Aspire ConnectionStrings:{Name}（通过 Aspire 集成自动注入）
/// 2. 环境变量：CacheMemory__Instances__{Name}__ConnectionString
/// 3. 环境变量：CacheMemory__Instances__{Name}__DefaultDatabase
/// 4. appsettings.json 或其他 IConfiguration 源
/// 5. 代码默认值
/// </remarks>
public class CacheMemoryConfig(IConfiguration configuration)
{
    private readonly IConfiguration _configuration =
        configuration ?? throw new ArgumentNullException(nameof(configuration));

    /// <summary>
    /// 从 Aspire 的 ConnectionStrings 配置节读取连接字符串，对接到 CacheMemoryOption 的指定实例。
    /// 这是 Aspire 集成桥接的核心方法：将 Aspire 的 ConnectionStrings:{name} 映射到 CacheMemoryOption.Instances[{instanceName}]。
    /// </summary>
    /// <param name="options">要填充的 CacheMemoryOption</param>
    /// <param name="connectionName">Aspire 连接名称（对应 ConnectionStrings:{connectionName}）</param>
    /// <param name="instanceName">CacheMemory 实例名称，默认 "Default"</param>
    public void ApplyAspireConnectionString(CacheMemoryOption options, string connectionName,
        string? instanceName = null)
    {
        var connString = _configuration.GetConnectionString(connectionName);
        if (string.IsNullOrWhiteSpace(connString))
        {
            // Aspire 未配置该连接，不覆盖已有配置
            return;
        }

        instanceName = string.IsNullOrWhiteSpace(instanceName)
            ? CacheMemoryOption.DefaultInstanceName
            : instanceName;

        // 确保目标实例存在
        if (!options.Instances.ContainsKey(instanceName))
            options.Instances[instanceName] = new RedisInstanceOptions();

        options.Instances[instanceName].ConnectionString = connString;
    }

    /// <summary>
    /// 从 Aspire 的 ConnectionStrings 配置节批量应用多个连接。
    /// 每个 ConnectionString 名称自动映射为同名的 CacheMemory 实例。
    /// </summary>
    /// <param name="options">要填充的 CacheMemoryOption</param>
    /// <param name="connectionNames">Aspire 连接名称集合</param>
    public void ApplyAspireConnectionStrings(CacheMemoryOption options, params string[] connectionNames)
    {
        foreach (var name in connectionNames)
        {
            ApplyAspireConnectionString(options, name, name);
        }
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
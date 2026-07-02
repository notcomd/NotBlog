using System.Text.Json;
using StackExchange.Redis;

namespace CacheMemory.Core;

/// <summary>
/// CacheMemory 的全局配置选项。
/// 支持多 Redis 实例配置、重试策略配置、JSON 序列化选项等。
/// </summary>
public class CacheMemoryOption
{
    /// <summary>
    /// 默认 Redis 实例的配置键名（appsettings.json 中的 section 名称和环境变量前缀）。
    /// </summary>
    public const string DefaultSectionName = "CacheMemory";

    /// <summary>
    /// 默认实例名称。
    /// </summary>
    public const string DefaultInstanceName = "Default";

    /// <summary>
    /// Redis 实例配置字典。键为实例名称，值为对应配置。
    /// 默认包含一个名为 "Default" 的实例。
    /// </summary>
    public Dictionary<string, RedisInstanceOptions> Instances { get; set; } = new()
    {
        [DefaultInstanceName] = new RedisInstanceOptions()
    };

    /// <summary>
    /// 全局重试策略配置。
    /// </summary>
    public RetryOptions Retry { get; set; } = new();

    /// <summary>
    /// JSON 序列化选项（用于 <see cref="ICacheMemory{TMemory}"/> 的序列化）。
    /// </summary>
    public JsonSerializerOptions? JsonSerializerOptions { get; set; }
}

/// <summary>
/// 单个 Redis 实例的连接配置。
/// </summary>
public class RedisInstanceOptions
{
    /// <summary>
    /// Redis 连接字符串。支持 StackExchange.Redis 的所有连接字符串格式。
    /// 可通过环境变量 {InstanceName}__ConnectionString 覆盖。
    /// </summary>
    public string ConnectionString { get; set; } = "localhost:6379";

    /// <summary>
    /// 默认数据库索引。
    /// </summary>
    public int DefaultDatabase { get; set; } = -1;

    /// <summary>
    /// 连接超时时间（毫秒）。
    /// </summary>
    public int ConnectTimeoutMs { get; set; } = 5000;

    /// <summary>
    /// 同步超时时间（毫秒）。
    /// </summary>
    public int SyncTimeoutMs { get; set; } = 5000;

    /// <summary>
    /// 是否允许管理员命令（如 FLUSHDB）。
    /// </summary>
    public bool AllowAdmin { get; set; } = false;

    /// <summary>
    /// 连接失败时的重试策略。为 null 时使用全局重试策略。
    /// </summary>
    public RetryOptions? Retry { get; set; }

    /// <summary>
    /// 构建 StackExchange.Redis 的 ConfigurationOptions。
    /// </summary>
    public ConfigurationOptions ToConfigurationOptions()
    {
        var options = ConfigurationOptions.Parse(ConnectionString);
        options.DefaultDatabase = DefaultDatabase;
        options.ConnectTimeout = ConnectTimeoutMs;
        options.SyncTimeout = SyncTimeoutMs;
        options.AllowAdmin = AllowAdmin;
        options.AbortOnConnectFail = false; // 连接失败不终止，由重试策略处理
        return options;
    }
}
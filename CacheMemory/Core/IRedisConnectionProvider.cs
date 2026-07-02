using StackExchange.Redis;

namespace CacheMemory.Core;

/// <summary>
/// Redis 连接提供者接口。
/// 负责管理与 Redis 的连接生命周期，支持单节点和集群模式，以及多实例场景。
/// </summary>
public interface IRedisConnectionProvider : IAsyncDisposable
{
    /// <summary>
    /// 获取默认 Redis 连接的多路复用器。
    /// </summary>
    IConnectionMultiplexer GetConnection();

    /// <summary>
    /// 按实例名称获取 Redis 连接（支持多实例）。
    /// </summary>
    /// <param name="instanceName">实例名称，为 null 或 "Default" 时返回默认实例</param>
    IConnectionMultiplexer GetConnection(string? instanceName);

    /// <summary>
    /// 获取默认实例的数据库。
    /// </summary>
    /// <param name="db">数据库索引，-1 表示使用默认</param>
    Task<IDatabase> GetDatabaseAsync(int db = -1);

    /// <summary>
    /// 按实例名称获取数据库。
    /// </summary>
    /// <param name="instanceName">实例名称</param>
    /// <param name="db">数据库索引</param>
    Task<IDatabase> GetDatabaseAsync(string instanceName, int db = -1);

    /// <summary>
    /// 获取默认实例的订阅者。
    /// </summary>
    ISubscriber GetSubscriber();

    /// <summary>
    /// 按实例名称获取订阅者。
    /// </summary>
    ISubscriber GetSubscriber(string? instanceName);

    /// <summary>
    /// 获取所有已注册的实例名称。
    /// </summary>
    IReadOnlyCollection<string> GetInstanceNames();
}
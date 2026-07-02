namespace CacheMemory.Core;

/// <summary>
/// 泛型缓存服务接口。
/// 提供对 <typeparamref name="TMemory"/> 类型实体的高级缓存操作，
/// 自动处理 JSON 序列化/反序列化。
/// </summary>
/// <typeparam name="TMemory">实现了 <see cref="IMemory"/> 标记接口的实体类型</typeparam>
public interface ICacheMemory<TMemory> where TMemory : IMemory
{
    /// <summary>
    /// 从缓存获取实体。
    /// </summary>
    Task<TMemory?> GetAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 将实体存入缓存。
    /// </summary>
    Task SetAsync(string key, TMemory value, TimeSpan? expiry = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 仅当键不存在时设置实体（原子操作）。
    /// </summary>
    /// <returns>true 表示设置成功（键原本不存在）；false 表示键已存在</returns>
    Task<bool> SetIfNotExistsAsync(string key, TMemory value, TimeSpan? expiry = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除缓存实体。
    /// </summary>
    Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查缓存键是否存在。
    /// </summary>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 刷新缓存实体的过期时间。
    /// </summary>
    Task<bool> RefreshAsync(string key, TimeSpan expiry, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量获取多个缓存实体。
    /// </summary>
    Task<IEnumerable<TMemory?>> GetManyAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量设置缓存实体，使用相同的过期时间。
    /// </summary>
    Task SetManyAsync(IDictionary<string, TMemory> entries, TimeSpan? expiry = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 同步获取实体。
    /// </summary>
    TMemory? Get(string key);

    /// <summary>
    /// 同步设置实体。
    /// </summary>
    void Set(string key, TMemory value, TimeSpan? expiry = null);

    /// <summary>
    /// 同步删除实体。
    /// </summary>
    bool Remove(string key);
}
namespace Message.Domain.IServices;

public interface ICacheService
{
    /// <summary>
    /// 从缓存中获取值
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>缓存值</returns>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 设置缓存值
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <param name="value">缓存值</param>
    /// <param name="expiration">过期时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 从缓存中移除值
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <summary>
    /// 从缓存中移除值
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查缓存中是否存在值
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否存在</returns>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 增加缓存值
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="value">增加值</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>增加后的值</returns>
    Task<long> IncrementAsync(string key, long value = 1, CancellationToken cancellationToken = default);

    Task SetHashAsync(string key, string field, string value, CancellationToken cancellationToken = default);
    Task<string?> GetHashAsync(string key, string field, CancellationToken cancellationToken = default);
    Task<bool> SetAddAsync(string key, string value, CancellationToken cancellationToken = default);
    Task<bool> SetRemoveAsync(string key, string value, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> SetMembersAsync(string key, CancellationToken cancellationToken = default);
}
namespace Identity.Domain.ICache;

public interface IIdentityCacheService
{
    /// <summary>
    ///  根据key 获取缓存信息
    /// </summary>
    /// <param name="key"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<string?> GetStringAsync(string key, CancellationToken cancellationToken);

    /// <summary>
    /// 写入缓存（无过期时间）。仅用于需要永久驻留的场景；验证码等敏感数据请使用带 TTL 的重载。
    /// </summary>
    Task SetStringAsync(string key, string value, CancellationToken cancellationToken);

    /// <summary>
    /// 写入缓存并指定过期时间（用于验证码等敏感数据，避免永久驻留 Redis）。
    /// </summary>
    Task SetStringAsync(string key, string value, TimeSpan ttl, CancellationToken cancellationToken);

    /// <summary>
    ///  删除写入的缓存
    /// </summary>
    /// <param name="key"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task RemoveAsync(string key, CancellationToken cancellationToken);
}

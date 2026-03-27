namespace Message.Domain.IServices;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
    Task<long> IncrementAsync(string key, long value = 1, CancellationToken cancellationToken = default);
    Task SetHashAsync(string key, string field, string value, CancellationToken cancellationToken = default);
    Task<string?> GetHashAsync(string key, string field, CancellationToken cancellationToken = default);
    Task<bool> SetAddAsync(string key, string value, CancellationToken cancellationToken = default);
    Task<bool> SetRemoveAsync(string key, string value, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> SetMembersAsync(string key, CancellationToken cancellationToken = default);
}



namespace Identity.Domain.ICache;

public interface IIdentityCacheService
{
    Task<string?> GetStringAsync(string key, CancellationToken cancellationToken);

    Task SetStringAsync(string key, string value, CancellationToken cancellationToken);

    Task RemoveAsync(string key, CancellationToken cancellationToken);
}

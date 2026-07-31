using CacheMemory.Core;
using Identity.Domain.ICache;
namespace Identity.Infrastructure.Cache;

public class IdentityCacheService : IIdentityCacheService
{
    private readonly IRedisCacheService _redisCacheService;
    public IdentityCacheService(IRedisCacheService redisCacheService)
    {
        _redisCacheService = redisCacheService;
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken)
    {
        await _redisCacheService.KeyDeleteAsync(key, cancellationToken);
    }

    public async Task<string?> GetStringAsync(string key, CancellationToken cancellationToken)
    {
        return await _redisCacheService.StringGetAsync(key, cancellationToken);
    }

    public async Task SetStringAsync(string key, string value, CancellationToken cancellationToken)
    {
        await _redisCacheService.StringSetAsync(key, value, ct: cancellationToken);
    }

}
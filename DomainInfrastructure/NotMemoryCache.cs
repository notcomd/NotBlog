using System.Text;

using DomainCommonst;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Notcomd.DomainCommand;

public class NotMemoryCache : INotMemoryCache
{

    private readonly IDistributedCache _memoryDistributedCache;
    private readonly ILogger<NotMemoryCache> _logger;

    public NotMemoryCache(IDistributedCache memoryDistributedCache, ILogger<NotMemoryCache> logger)
    {
        _memoryDistributedCache = memoryDistributedCache;
        _logger = logger;
    }

    public async ValueTask AddByMemoryCacheAsync(string key, byte[] value, long expiredTimeMinutes = 5)
    {
        try
        {
            if (string.IsNullOrEmpty(key) || value == null)
                return;
            var cacheEntryOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(expiredTimeMinutes) // Set expiration time as needed
            };

            await _memoryDistributedCache.SetAsync(key, value, cacheEntryOptions);
            _logger.LogInformation($"Added item with key '{key}' to memory cache.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error adding item with key '{key}' to memory cache.");
            throw;
        }
    }

    public async ValueTask AddByMemoryCacheAsync(string key, string value, long expiredTimeMinutes = 5)
    {
        try
        {
            if (string.IsNullOrEmpty(key) || value == null)
                return;
            var cacheEntryOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(expiredTimeMinutes) // Set expiration time as needed
            };

            await _memoryDistributedCache.SetAsync(key, Encoding.UTF8.GetBytes(value), cacheEntryOptions);
            _logger.LogInformation($"Added item with key '{key}' to memory cache.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error adding item with key '{key}' to memory cache.");
            throw;
        }
    }

    public async ValueTask<string?> GetByMemoryCacheAsync(string key)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                _logger.LogWarning("Key is null or empty, cannot retrieve item from memory cache.");
                return string.Empty;
            }
            var codeByte = await _memoryDistributedCache.GetAsync(key);
            if (codeByte is null || codeByte.Length == 0)
            {
                _logger.LogInformation($"Item with key '{key}' not found in memory cache.");
                return string.Empty;
            }
            var code = Encoding.UTF8.GetString(codeByte);
            return code;

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving item with key '{key}' from memory cache.");
            throw;
        }
    }


    public async ValueTask RemoveByMemoryCacheAsync(string key)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                _logger.LogWarning("Key is null or empty, cannot remove item from memory cache.");
                return;
            }
            await _memoryDistributedCache.RemoveAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error removing item with key '{key}' from memory cache.");
            throw;
        }
    }

    public async ValueTask<bool> IsValidateCodeAsync(string key, string vlaue)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(vlaue))
            {
                _logger.LogWarning("Key or value is null or empty, cannot validate code.");
                return false;
            }
            var cachedCode = await GetByMemoryCacheAsync(key);
            if (string.IsNullOrEmpty(cachedCode))
            {
                return false;
            }
            if (cachedCode.Equals(vlaue))
            {
                await RemoveByMemoryCacheAsync(key);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error validating code with key '{key}' from memory cache.");
            throw;
        }
    }


    public async ValueTask<bool> IsExistsAsync(string key)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                _logger.LogWarning("Key is null or empty, cannot check existence in memory cache.");
                return false;
            }
            var codeByte = await _memoryDistributedCache.GetAsync(key);
            var exists = codeByte != null && codeByte.Length > 0;
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error checking existence of item with key '{key}' in memory cache.");
            throw;
        }
    }
}

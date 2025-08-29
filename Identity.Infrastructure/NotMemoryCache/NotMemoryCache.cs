


namespace Identity.Infrastructure.NotMemoryCache;

public class NotMemoryCache : INotMemoryCache
{

    private readonly IDistributedCache _memoryCache;

    private readonly ILogger<NotMemoryCache> _logger;



    public NotMemoryCache(IDistributedCache memoryCache, ILogger<NotMemoryCache> logger)
    {
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }


    public Task AddByMemoryCacheAsync(string key, byte[] value, long expiredTimeMinutes = 5)
    {
        if (key != null && value != null)
        {
            var cacheEntryOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(expiredTimeMinutes) // Set expiration time as needed
            };
            _logger.LogInformation($"Added item with key '{key}' to memory cache.");
            return _memoryCache.SetAsync(key, value, cacheEntryOptions);
        }
        return Task.CompletedTask;
    }


    public Task AddByMemoryCacheAsync(string key, string value, long expiredTimeMinutes = 5)
    {

        if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
        {
            var byteValue = Encoding.UTF8.GetBytes(value);
            var cacheEntryOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(expiredTimeMinutes) // Set expiration time as needed
            };
            _logger.LogInformation($"Added item with key '{key}' to memory cache.");
            return _memoryCache.SetAsync(key, byteValue, cacheEntryOptions);
        }
        return Task.CompletedTask;
    }

    public async Task<string?> GetByMemoryCacheAsync(string key)
    {
        if (key == string.Empty)
        {
            _logger.LogWarning("Key is null, cannot retrieve item from memory cache.");
            return string.Empty;
        }
        var codeByte = await _memoryCache.GetAsync(key);
        if(codeByte is null||codeByte.Length==0)
        {
             _logger.LogInformation($"Item with key '{key}' not found in memory cache.");
             return string.Empty;
        }

        var code = Encoding.UTF8.GetString(codeByte);
        return code;
    }

    public Task RemoveByMemoryCacheAsync(string key)
    {
        return _memoryCache.RemoveAsync(key);
    }

    public async Task<bool> IsValidateCodeAsync(string key, string checkGenerate)
    {
        if (key == null || checkGenerate == null)
        {
            _logger.LogWarning("Key or value is null, cannot validate code.");
            return false;
        }
        var cachedCode = await GetByMemoryCacheAsync(key);
        if (string.IsNullOrEmpty(cachedCode))
        {
            return false;
        }
        if (cachedCode == checkGenerate)
        {
            await RemoveByMemoryCacheAsync(key);
            return true;
        }
        return false;
    }

    public async ValueTask<bool> IsExistsAsync(string key)
    {
       if(string.IsNullOrEmpty(key))
           return false;
        var value = await _memoryCache.GetAsync(key);
        if(value is null || value.Length == 0)
            return false;
        return true;
    }
}

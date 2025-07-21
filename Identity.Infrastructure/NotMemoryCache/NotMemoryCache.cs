using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Infrastructure.NotMemoryCache
{
    public class NotMemoryCache: INotMemoryCache
    {

        private readonly MemoryCache _memoryCache;

        private readonly ILogger<NotMemoryCache> _logger;

        public NotMemoryCache(MemoryCache memoryCache, ILogger<NotMemoryCache> logger)
        {
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task AddByMemoryCacheAsync(object key, object value, long expiredTimeMinutes = 5)
        {
            if(key != null && value != null)
            {
                var cacheEntryOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(expiredTimeMinutes) // Set expiration time as needed
                };
                _memoryCache.Set(key, value, cacheEntryOptions);
                _logger.LogInformation($"Added item with key '{key}' to memory cache.");
                return Task.CompletedTask;
            }
           return Task.CompletedTask;
        }

        public Task<T?> GetByMemoryCacheAsync<T>(object key) where T : class
        {
            if (key == null)
            {
                _logger.LogWarning("Key is null, cannot retrieve item from memory cache.");
                return Task.FromResult<T?>(null);
            }
            return _memoryCache.Get(key) is not null
                ? Task.FromResult<T?>(_memoryCache.Get<T>(key))
                : Task.FromResult<T?>(null);

        }

        public Task RemoveByMemoryCacheAsync(object key)
        {
            this._memoryCache.Remove(key);
            return Task.CompletedTask;
        }

        public Task<bool> ValidateCodeAsync(object key, object vlaue)
        {
            if(key == null || vlaue == null)
            {
                _logger.LogWarning("Key or value is null, cannot validate code.");
                return Task.FromResult(false);
            }
            var cachedCode = _memoryCache.Get<object>(key);
            return Task.FromResult(cachedCode != null && cachedCode.Equals(vlaue));

        }
    }
}

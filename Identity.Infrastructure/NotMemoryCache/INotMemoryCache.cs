namespace Identity.Infrastructure.NotMemoryCache
{
    public interface INotMemoryCache
    {
        Task AddByMemoryCacheAsync(object key, object value, long expiredTimeMinutes = 5);

        Task<T?> GetByMemoryCacheAsync<T>(object key) where T : class;

        Task RemoveByMemoryCacheAsync(object key);

        Task<bool> ValidateCodeAsync(object key, object vlaue);
    }
}

namespace Identity.Domain
    .INotMemoryCache
{
    public interface INotMemoryCache
    {
        Task AddByMemoryCacheAsync(string key, byte[] value, long expiredTimeMinutes = 5);

        Task AddByMemoryCacheAsync(string key, string value, long expiredTimeMinutes = 5);

        Task<string?> GetByMemoryCacheAsync(string key);

        Task RemoveByMemoryCacheAsync(string key);

        Task<bool> IsValidateCodeAsync(string key, string vlaue);

        ValueTask<bool> IsExistsAsync(string key);
    }
}

namespace DomainCommon;

public interface INotMemoryCache
{

    ValueTask AddByMemoryCacheAsync(string key, byte[] value, long expiredTimeMinutes = 5);

    ValueTask AddByMemoryCacheAsync(string key, string value, long expiredTimeMinutes = 5);

    ValueTask<string?> GetByMemoryCacheAsync(string key);

    ValueTask RemoveByMemoryCacheAsync(string key);

    ValueTask<bool> IsValidateCodeAsync(string key, string vlaue);

    ValueTask<bool> IsExistsAsync(string key);

}

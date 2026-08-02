using System.Text.Json;

namespace Identity.Infrastructure.Repository;

public class MemoryRepository<TModel>(IDistributedCache redisCache) : IMemoryRepository<TModel> where TModel:class
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public async Task SetStringAsync(string key, string value, TimeSpan? expiration = null)
    {
        var options = new DistributedCacheEntryOptions();
        if (expiration.HasValue)
            options.AbsoluteExpirationRelativeToNow = expiration.Value;
        
        await redisCache.SetStringAsync(key, value, options);
    }

    public async Task<string?> GetStringAsync(string key)
    {
        return await redisCache.GetStringAsync(key);
    }

    public async Task SetObjectAsync(string key, TModel obj, TimeSpan? expiration = null)
    {
        var json = JsonSerializer.Serialize(obj, JsonOptions);
        await SetStringAsync(key, json, expiration);
    }

    public async Task<TModel?> GetObjectAsync(string key)
    {
        var json = await GetStringAsync(key);
        if (string.IsNullOrEmpty(json))
            return default;
        
        return JsonSerializer.Deserialize<TModel>(json, JsonOptions);
    }

    public async Task<bool> KeyExistsAsync(string key)
    {
        return await redisCache.GetAsync(key) != null;
    }

    public async Task<bool> DeleteKeyAsync(string key)
    {
        await redisCache.RemoveAsync(key);
        return true;
    }

    public Task<int> DeleteKeysByPatternAsync(string pattern)
    {
        throw new NotImplementedException("Redis 集群模式不支持批量删除，需要单独实现");
    }

    public async Task<bool> SetExpirationAsync(string key, TimeSpan expiration)
    {
        var entry = await redisCache.GetAsync(key);
        if (entry == null)
            return false;
        
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration
        };
        
        await redisCache.SetAsync(key, entry, options);
        return true;
    }

    public Task<TimeSpan?> GetTtlAsync(string key)
    {
        throw new NotImplementedException("IDistributedCache 不支持直接获取 TTL");
    }

    public Task<long> IncrementAsync(string key, long value = 1)
    {
        throw new NotImplementedException("需要使用 IDatabase 接口实现");
    }

    public Task<long> DecrementAsync(string key, long value = 1)
    {
        throw new NotImplementedException("需要使用 IDatabase 接口实现");
    }

    public async Task HashSetAsync(string key, string field, string value)
    {
        var hash = await HashGetAllAsync(key);
        hash[field] = value;
        await SetObjectAsync(key, (TModel)(object)hash);
    }

    public async Task<string?> HashGetAsync(string key, string field)
    {
        var hash = await HashGetAllAsync(key);
        return hash.TryGetValue(field, out var value) ? value : null;
    }

    public async Task<Dictionary<string, string>> HashGetAllAsync(string key)
    {
        var json = await GetStringAsync(key);
        if (string.IsNullOrEmpty(json))
            return new Dictionary<string, string>();
        
        return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
    }

    public async Task<bool> HashDeleteAsync(string key, string field)
    {
        var hash = await HashGetAllAsync(key);
        if (!hash.ContainsKey(field))
            return false;
        
        hash.Remove(field);
        await SetObjectAsync(key, (TModel)(object)hash);
        return true;
    }

    public async Task<long> ListLeftPushAsync(string key, string value)
    {
        var list = await GetListAsync(key);
        list.Insert(0, value);
        await SetListAsync(key, list);
        return list.Count;
    }

    public async Task<string?> ListRightPopAsync(string key)
    {
        var list = await GetListAsync(key);
        if (!list.Any())
            return null;
        
        var last = list.Last();
        list.RemoveAt(list.Count - 1);
        await SetListAsync(key, list);
        return last;
    }

    public async Task<List<string>> ListRangeAsync(string key, long start = 0, long stop = -1)
    {
        var list = await GetListAsync(key);
        if (stop == -1)
            stop = list.Count - 1;
        
        return list.Skip((int)start).Take((int)(stop - start + 1)).ToList();
    }

    public async Task<long> SetAddAsync(string key, string member)
    {
        var set = await GetSetAsync(key);
        if (!set.Add(member))
            return set.Count;
        
        await SetSetAsync(key, set);
        return set.Count;
    }

    public async Task<bool> SetIsMemberAsync(string key, string member)
    {
        var set = await GetSetAsync(key);
        return set.Contains(member);
    }

    public async Task<HashSet<string>> SetMembersAsync(string key)
    {
        return await GetSetAsync(key);
    }

    public Task<bool> SortedSetAddAsync(string key, string member, double score)
    {
        // TODO(F-07): 需要基于 IDatabase.SortedSetAddAsync 单独实现
        throw new NotImplementedException("需要使用 IDatabase 接口实现有序集合");
    }

    public Task<IEnumerable<string>> SortedSetRangeByRankAsync(string key, long start = 0, long stop = -1)
    {
        // TODO(F-07): 需要基于 IDatabase.SortedSetRangeByRankAsync 单独实现
        throw new NotImplementedException("需要使用 IDatabase 接口实现有序集合");
    }

    public Task<long> PublishAsync(string channel, string message)
    {
        // TODO(F-07): 需要基于 ISubscriber 接口单独实现
        throw new NotImplementedException("需要使用 ISubscriber 接口实现发布订阅");
    }

    public Task FlushAllAsync()
    {
        // TODO(F-07): 生产环境禁止清空缓存，仅允许在受控维护窗口通过运维工具执行
        throw new NotImplementedException("生产环境禁止使用此方法");
    }

    private async Task<List<string>> GetListAsync(string key)
    {
        var json = await GetStringAsync(key);
        return string.IsNullOrEmpty(json) 
            ? new List<string>() 
            : JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
    }

    private async Task SetListAsync(string key, List<string> list)
    {
        var json = JsonSerializer.Serialize(list, JsonOptions);
        await SetStringAsync(key, json);
    }

    private async Task<HashSet<string>> GetSetAsync(string key)
    {
        var json = await GetStringAsync(key);
        return string.IsNullOrEmpty(json) 
            ? new HashSet<string>() 
            : JsonSerializer.Deserialize<HashSet<string>>(json) ?? new HashSet<string>();
    }

    private async Task SetSetAsync(string key, HashSet<string> set)
    {
        var json = JsonSerializer.Serialize(set, JsonOptions);
        await SetStringAsync(key, json);
    }
}
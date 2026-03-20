namespace Identity.Domain.IRepository;


public interface IMemoryRepository<T> where T : class
{
    /// <summary>
    /// 设置字符串值
    /// </summary>
    Task SetStringAsync(string key, string value, TimeSpan? expiration = null);
    
    /// <summary>
    /// 获取字符串值
    /// </summary>
    Task<string?> GetStringAsync(string key);
    
    /// <summary>
    /// 设置对象（序列化为 JSON）
    /// </summary>
    Task SetObjectAsync(string key, T obj, TimeSpan? expiration = null);
    
    /// <summary>
    /// 获取对象（反序列化为 T）
    /// </summary>
    Task<T?> GetObjectAsync(string key);
    
    /// <summary>
    /// 检查键是否存在
    /// </summary>
    Task<bool> KeyExistsAsync(string key);
    
    /// <summary>
    /// 删除键
    /// </summary>
    Task<bool> DeleteKeyAsync(string key);
    
    /// <summary>
    /// 批量删除匹配模式的键
    /// </summary>
    Task<int> DeleteKeysByPatternAsync(string pattern);
    
    /// <summary>
    /// 设置过期时间
    /// </summary>
    Task<bool> SetExpirationAsync(string key, TimeSpan expiration);
    
    /// <summary>
    /// 获取剩余过期时间
    /// </summary>
    Task<TimeSpan?> GetTtlAsync(string key);
    
    /// <summary>
    /// 自增操作
    /// </summary>
    Task<long> IncrementAsync(string key, long value = 1);
    
    /// <summary>
    /// 自减操作
    /// </summary>
    Task<long> DecrementAsync(string key, long value = 1);
    
    /// <summary>
    /// 哈希表 - 设置字段值
    /// </summary>
    Task HashSetAsync(string key, string field, string value);
    
    /// <summary>
    /// 哈希表 - 获取字段值
    /// </summary>
    Task<string?> HashGetAsync(string key, string field);
    
    /// <summary>
    /// 哈希表 - 获取所有字段
    /// </summary>
    Task<Dictionary<string, string>> HashGetAllAsync(string key);
    
    /// <summary>
    /// 哈希表 - 删除字段
    /// </summary>
    Task<bool> HashDeleteAsync(string key, string field);
    
    /// <summary>
    /// 列表 - 左侧推入
    /// </summary>
    Task<long> ListLeftPushAsync(string key, string value);
    
    /// <summary>
    /// 列表 - 右侧弹出
    /// </summary>
    Task<string?> ListRightPopAsync(string key);
    
    /// <summary>
    /// 列表 - 获取范围
    /// </summary>
    Task<List<string>> ListRangeAsync(string key, long start = 0, long stop = -1);
    
    /// <summary>
    /// 集合 - 添加成员
    /// </summary>
    Task<long> SetAddAsync(string key, string member);
    
    /// <summary>
    /// 集合 - 判断是否包含成员
    /// </summary>
    Task<bool> SetIsMemberAsync(string key, string member);
    
    /// <summary>
    /// 集合 - 获取所有成员
    /// </summary>
    Task<HashSet<string>> SetMembersAsync(string key);
    
    /// <summary>
    /// 有序集合 - 添加成员
    /// </summary>
    Task<bool> SortedSetAddAsync(string key, string member, double score);
    
    /// <summary>
    /// 有序集合 - 获取排名范围内的成员
    /// </summary>
    Task<IEnumerable<string>> SortedSetRangeByRankAsync(string key, long start = 0, long stop = -1);
    
    /// <summary>
    /// 发布消息到频道
    /// </summary>
    Task<long> PublishAsync(string channel, string message);
    
    /// <summary>
    /// 清空所有数据库（生产环境慎用）
    /// </summary>
    Task FlushAllAsync();
}

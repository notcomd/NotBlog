namespace CacheMemory.Core;

/// <summary>
/// 标记接口，用于标识可存储到 Redis 的实体类型。
/// 实现此接口的类表明它是一个内存对象，可被 <see cref="ICacheMemory{TMemory}"/> 服务处理。
/// </summary>
/// <remarks>
/// 典型用法：
/// <code>
/// public class UserEntity : IMemory
/// {
///     public string Id { get; set; }
///     public string Name { get; set; }
/// }
/// </code>
/// </remarks>
public interface IMemory
{
    /// <summary>
    /// 获取缓存键。可选实现，若未实现则需在调用时手动指定键。
    /// </summary>
    string? CacheKey => null;
}
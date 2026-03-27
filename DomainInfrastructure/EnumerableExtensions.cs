namespace DomainInfrastructure;

/// <summary>
/// IEnumerable 扩展方法
/// </summary>
public static class EnumerableExtensions
{
    /// <summary>
    /// 忽略顺序比较两个序列是否相等
    /// </summary>
    public static bool SequenceIgnoredEqual<T>(this IEnumerable<T> source, IEnumerable<T> other)
    {
        if (ReferenceEquals(source, other))
            return true;

        if (source is null || other is null)
            return false;

        return source.OrderBy(x => x).SequenceEqual(other.OrderBy(x => x));
    }
}
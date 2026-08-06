namespace Message.Domain.IServices;

/// <summary>
/// 敏感词过滤服务。
/// <para>当前默认实现仅记录日志直接放行，后续可替换为第三方敏感词库实现。</para>
/// </summary>
public interface ISensitiveWordFilter
{
    /// <summary>过滤文本中的敏感词</summary>
    /// <param name="content">待过滤内容</param>
    /// <returns>过滤结果（Passed = 是否通过）</returns>
    Task<FilterResult> FilterAsync(string content);
}

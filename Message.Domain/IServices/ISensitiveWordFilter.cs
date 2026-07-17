using Message.Domain.Entities.Tweet;

namespace Message.Domain.IServices;

/// <summary>
/// 敏感词过滤服务接口
/// 当前默认实现仅记录日志放行，后续可接入敏感词库实现实际过滤
/// </summary>
public interface ISensitiveWordFilter
{
    /// <summary>
    /// 过滤文本中的敏感词
    /// </summary>
    /// <param name="content">待检测的文本内容</param>
    /// <returns>过滤结果（包含是否通过、命中词列表）</returns>
    Task<FilterResult> FilterAsync(string content);
}

public record FilterResult(bool Passed, IReadOnlyList<string> MatchedWords);

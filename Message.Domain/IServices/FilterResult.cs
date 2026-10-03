
namespace Message.Domain.IServices;
/// <summary>
/// 敏感词过滤结果。
/// </summary>
/// <param name="Passed">是否通过（未命中敏感词）</param>
/// <param name="MatchedWords">命中的敏感词列表</param>
public record FilterResult(bool Passed, IReadOnlyList<string> MatchedWords);

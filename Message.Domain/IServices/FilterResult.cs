
namespace Message.Domain.IServices;
public record FilterResult(bool Passed, IReadOnlyList<string> MatchedWords);

namespace Message.Domain.IServices;
/// <summary>
/// 图片审核结果。
/// </summary>
/// <param name="Passed">是否通过</param>
/// <param name="Reason">未通过原因（通过时为 null）</param>
public record ModerationResult(bool Passed, string? Reason);

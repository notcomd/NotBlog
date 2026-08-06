namespace Message.Domain.IServices;

/// <summary>
/// 图片审核服务（审核图片 URL 列表，返回是否通过）。
/// <para>当前默认实现仅记录日志直接放行，后续可替换为第三方审核 API。</para>
/// </summary>
public interface IImageModerationService
{
    /// <summary>审核图片 URL 列表</summary>
    /// <param name="mediaUrls">图片 URL 列表</param>
    /// <returns>审核结果（Passed = 是否通过）</returns>
    Task<ModerationResult> ModerateAsync(IEnumerable<string> mediaUrls);
}

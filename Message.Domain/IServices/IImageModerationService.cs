namespace Message.Domain.IServices;

/// <summary>
/// 图片内容审核服务接口
/// 当前默认实现仅记录日志放行，后续可接入第三方审核 API
/// </summary>
public interface IImageModerationService
{
    /// <summary>
    /// 审核媒体内容（图片/视频）
    /// </summary>
    /// <param name="mediaUrls">媒体文件 URL 列表</param>
    /// <returns>审核结果</returns>
    Task<ModerationResult> ModerateAsync(IEnumerable<string> mediaUrls);
}

public record ModerationResult(bool Passed, string? Reason);

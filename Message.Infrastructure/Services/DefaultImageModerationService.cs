namespace Message.Infrastructure.Services;

/// <summary>
/// 图片审核默认实现 — 仅记录日志，直接放行
/// 后续可替换为接入第三方审核 API（阿里云/AWS等）的实现
/// </summary>
public class DefaultImageModerationService : IImageModerationService
{
    private readonly ILogger<DefaultImageModerationService> _logger;

    /// <summary>初始化 <see cref="DefaultImageModerationService"/> 实例。</summary>
    /// <param name="logger">日志记录器。</param>
    public DefaultImageModerationService(ILogger<DefaultImageModerationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>对媒体资源进行审核；当前默认实现仅记录日志并直接放行。</summary>
    public Task<ModerationResult> ModerateAsync(IEnumerable<string> mediaUrls)
    {
        var urls = mediaUrls.ToList();
        _logger.LogInformation("[预留] 图片审核: 当前跳过检测, MediaCount={Count}", urls.Count);
        return Task.FromResult(new ModerationResult(true, null));
    }
}

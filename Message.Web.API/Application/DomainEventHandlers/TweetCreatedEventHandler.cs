
namespace Message.Web.API.Application.DomainEventHandlers;

public class TweetCreatedEventHandler : INotificationHandler<TweetCreatedEvent>
{
    private readonly ISensitiveWordFilter _sensitiveWordFilter;
    private readonly IImageModerationService _imageModerationService;
    private readonly ILogger<TweetCreatedEventHandler> _logger;

    public TweetCreatedEventHandler(
        ISensitiveWordFilter sensitiveWordFilter,
        IImageModerationService imageModerationService,
        ILogger<TweetCreatedEventHandler> logger)
    {
        _sensitiveWordFilter = sensitiveWordFilter;
        _imageModerationService = imageModerationService;
        _logger = logger;
    }

    public async Task Handler(TweetCreatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("[{Time}] 推文创建事件: TweetGuid={TweetGuid}, AuthorGuid={AuthorGuid}",
                DateTimeOffset.UtcNow, notification.TweetGuid, notification.AuthorGuid);

            // 敏感词过滤（当前仅记录日志，后续会触发自动审核）
            var content = string.Empty;
            var filterResult = await _sensitiveWordFilter.FilterAsync(content);
            _logger.LogInformation("敏感词过滤结果: Passed={Passed}, MatchedWords={MatchedWords}",
                filterResult.Passed, string.Join(",", filterResult.MatchedWords));

            // 图片审核（当前仅记录日志，后续会触发自动审核）
            var mediaUrls = Array.Empty<string>();
            var moderationResult = await _imageModerationService.ModerateAsync(mediaUrls);
            _logger.LogInformation("图片审核结果: Passed={Passed}, Reason={Reason}",
                moderationResult.Passed, moderationResult.Reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理推文创建事件失败: TweetGuid={TweetGuid}", notification.TweetGuid);
        }
    }
}

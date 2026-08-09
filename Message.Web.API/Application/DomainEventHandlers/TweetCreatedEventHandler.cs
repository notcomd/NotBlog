namespace Message.Web.API.Application.DomainEventHandlers;

public class TweetCreatedEventHandler(
    ITweetRepository tweetRepository,
    ISensitiveWordFilter sensitiveWordFilter,
    IImageModerationService imageModerationService,
    ILogger<TweetCreatedEventHandler> logger) : INotificationHandler<TweetCreatedEvent>
{
    public async Task Handler(TweetCreatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("[{Time}] 推文创建事件: TweetGuid={TweetGuid}, AuthorGuid={AuthorGuid}",
                DateTimeOffset.UtcNow, notification.TweetGuid, notification.AuthorGuid);

            // R-05：读取真实推文内容与媒体 URL（此前传空字符串/空数组，审核链路空转）
            var tweet = await tweetRepository.GetByIdAsync(notification.TweetGuid);
            if (tweet is null)
            {
                logger.LogWarning("推文创建事件处理时推文不存在: TweetGuid={TweetGuid}", notification.TweetGuid);
                return;
            }

            // 敏感词过滤（默认实现仅记录日志放行；后续替换为真实审核服务实现即生效，无需改此链路）
            var filterResult = await sensitiveWordFilter.FilterAsync(tweet.Content);
            logger.LogInformation("敏感词过滤结果: Passed={Passed}, MatchedWords={MatchedWords}",
                filterResult.Passed, string.Join(",", filterResult.MatchedWords));

            // 图片审核（默认实现仅记录日志放行；MediaUrl 为 FileDev 服务端解析的媒体 URI）
            var mediaUrls = tweet.Media
                .Select(m => m.MediaUrl)
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .ToArray();
            var moderationResult = await imageModerationService.ModerateAsync(mediaUrls);
            logger.LogInformation("图片审核结果: Passed={Passed}, Reason={Reason}",
                moderationResult.Passed, moderationResult.Reason);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "处理推文创建事件失败: TweetGuid={TweetGuid}", notification.TweetGuid);
        }
    }
}

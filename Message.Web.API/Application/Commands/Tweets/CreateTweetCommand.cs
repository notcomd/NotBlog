namespace Message.Web.API.Application.Commands.Tweets;

/// <summary>
/// 创建推文命令。
/// <para>CQRS 命令侧：仅返回新推文的标识（Guid），不返回业务实体/DTO。</para>
/// </summary>
/// <param name="UserId">作者用户 ID</param>
/// <param name="Content">推文内容</param>
/// <param name="MediaUrls">媒体 URL 列表</param>
/// <param name="LinkUrl">链接 URL</param>
/// <param name="Visibility">可见性</param>
public record CreateTweetCommand(
    Guid UserId,
    string Content,
    IEnumerable<string>? MediaUrls,
    string? LinkUrl,
    Visibility Visibility) : IRequest<Guid>;

/// <summary>
/// 创建推文命令处理程序。
/// </summary>
public class CreateTweetCommandHandler(
    ITweetRepository tweetRepository,
    ISensitiveWordFilter sensitiveWordFilter,
    IImageModerationService imageModerationService,
    ILogger<CreateTweetCommandHandler> logger) : IRequestHandler<CreateTweetCommand, Guid>
{
    public async Task<Guid> Handler(CreateTweetCommand command, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("开始创建推文，作者: {AuthorGuid}", command.UserId);

            var linkMetadata = CreateLinkMetadata(command.LinkUrl);
            var parsedVisibility = ParseVisibility(command.Visibility.ToString());
            var tweet = Tweet.Create(command.UserId, command.Content, null, linkMetadata, null, parsedVisibility);

            // 敏感词过滤（仅记录日志）
            try
            {
                var filterResult = await sensitiveWordFilter.FilterAsync(command.Content);
                if (!filterResult.Passed)
                {
                    logger.LogWarning("推文包含敏感词，命中词: {MatchedWords}",
                        string.Join(", ", filterResult.MatchedWords));
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "敏感词过滤失败，继续创建推文");
            }

            // 图片审核（仅记录日志）
            if (command.MediaUrls is not null && command.MediaUrls.Any())
            {
                try
                {
                    var moderationResult = await imageModerationService.ModerateAsync(command.MediaUrls);
                    if (!moderationResult.Passed)
                    {
                        logger.LogWarning("图片审核未通过，原因: {Reason}", moderationResult.Reason);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "图片审核失败，继续创建推文");
                }
            }

            await tweetRepository.AddAsync(tweet);
            await tweetRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            logger.LogInformation("推文创建成功，ID: {TweetGuid}", tweet.TweetGuid);
            logger.LogInformation("创建推文成功：{TweetGuid}", tweet.TweetGuid);
            return tweet.TweetGuid;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "创建推文失败，作者: {AuthorGuid}", command.UserId);
            throw;
        }
    }

    private static LinkMetadata? CreateLinkMetadata(string? linkUrl) =>
        string.IsNullOrWhiteSpace(linkUrl) ? null : LinkMetadata.Create(linkUrl);

    private static Visibility ParseVisibility(string? visibility) =>
        visibility?.ToLower() switch
        {
            "followers" => Visibility.Followers,
            "private" => Visibility.Private,
            _ => Visibility.Public
        };
}

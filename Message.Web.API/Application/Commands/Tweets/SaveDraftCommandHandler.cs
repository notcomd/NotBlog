namespace Message.Web.API.Application.Commands.Tweets;
/// <summary>
/// 保存推文草稿命令处理程序。
/// </summary>
public class SaveDraftCommandHandler(
    ITweetRepository tweetRepository,
    ILogger<SaveDraftCommandHandler> logger) : IRequestHandler<SaveDraftCommand, Guid>
{
    public async Task<Guid> Handler(SaveDraftCommand command, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("开始保存草稿，作者: {AuthorGuid}", command.UserId);

            var linkMetadata = CreateLinkMetadata(command.LinkUrl);
            var parsedVisibility = ParseVisibility(command.Visibility.ToString());

            // S-17：内容净化 + 长度校验（上限 500 字符）+ 敏感词拒绝（与发布策略一致）
            var safeContent = SafeContentSanitizer.Sanitize(command.Content);
            if (safeContent.Length > 500)
                throw new ArgumentException("推文内容不能超过500个字符");
            var (isSensitive, matchedWord) = SensitiveWordFilter.ContainsSensitive(safeContent);
            if (isSensitive)
                throw new InvalidOperationException($"推文内容包含敏感内容（{matchedWord}），已拒绝保存");

            var tweet = Tweet.Create(command.UserId, safeContent, null, linkMetadata, null, parsedVisibility);

            await tweetRepository.AddAsync(tweet);
            await tweetRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            logger.LogInformation("草稿保存成功，ID: {TweetGuid}", tweet.TweetGuid);
            logger.LogInformation("保存草稿成功：{TweetGuid}", tweet.TweetGuid);
            return tweet.TweetGuid;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "保存草稿失败，作者: {AuthorGuid}", command.UserId);
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

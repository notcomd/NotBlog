namespace Message.Web.API.Application.Commands.Tweets;

/// <summary>
/// 更新推文草稿命令。
/// <para>CQRS 命令侧：仅返回操作结果（bool），不返回业务实体/DTO。</para>
/// </summary>
/// <param name="TweetGuid">推文（草稿）ID</param>
/// <param name="UserId">作者用户 ID</param>
/// <param name="Content">更新后的内容</param>
/// <param name="FileIds">FileDev 文件 ID 列表（草稿暂不落媒体，保留字段）</param>
/// <param name="LinkUrl">链接 URL</param>
/// <param name="Visibility">可见性</param>
public record UpdateDraftCommand(
    Guid TweetGuid,
    Guid UserId,
    string Content,
    IEnumerable<Guid>? FileIds,
    string? LinkUrl,
    Visibility Visibility) : IRequest<bool>;

/// <summary>
/// 更新推文草稿命令处理程序。
/// </summary>
public class UpdateDraftCommandHandler(
    ITweetRepository tweetRepository,
    ILogger<UpdateDraftCommandHandler> logger) : IRequestHandler<UpdateDraftCommand, bool>
{
    public async Task<bool> Handler(UpdateDraftCommand command, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("开始更新草稿，推文: {TweetGuid}", command.TweetGuid);

            var linkMetadata = CreateLinkMetadata(command.LinkUrl);
            var parsedVisibility = ParseVisibility(command.Visibility.ToString());

            var tweet = await tweetRepository.GetByIdAsync(command.TweetGuid);
            if (tweet == null)
                throw new KeyNotFoundException("推文不存在");

            if (tweet.AuthorGuid != command.UserId)
                throw new UnauthorizedAccessException("无权修改此推文");

            // S-17：内容净化 + 长度校验（上限 500 字符）+ 敏感词拒绝（与发布策略一致）
            var safeContent = SafeContentSanitizer.Sanitize(command.Content);
            if (safeContent.Length > 500)
                throw new ArgumentException("推文内容不能超过500个字符");
            var (isSensitive, matchedWord) = SensitiveWordFilter.ContainsSensitive(safeContent);
            if (isSensitive)
                throw new InvalidOperationException($"推文内容包含敏感内容（{matchedWord}），已拒绝更新");

            tweet.UpdateContent(safeContent);
            await tweetRepository.UpdateAsync(tweet);
            await tweetRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            logger.LogInformation("草稿更新成功，ID: {TweetGuid}", command.TweetGuid);
            logger.LogInformation("更新草稿成功：{TweetGuid}", command.TweetGuid);
            return true;
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not UnauthorizedAccessException)
        {
            logger.LogError(ex, "更新草稿失败，推文: {TweetGuid}", command.TweetGuid);
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

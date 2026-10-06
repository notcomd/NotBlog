namespace Message.Web.API.Application.Commands.Tweets;
/// <summary>
/// 创建推文命令处理程序。
/// </summary>
public class CreateTweetCommandHandler(
    ITweetRepository tweetRepository,
    ISensitiveWordFilter sensitiveWordFilter,
    IImageModerationService imageModerationService,
    Message.Web.API.Grpc.IFileStorageGrpcClient fileStorage,
    ILogger<CreateTweetCommandHandler> logger) : IRequestHandler<CreateTweetCommand, Guid>
{
    public async Task<Guid> Handler(CreateTweetCommand command, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("开始创建推文，作者: {AuthorGuid}", command.UserId);

            var linkMetadata = CreateLinkMetadata(command.LinkUrl);
            var parsedVisibility = ParseVisibility(command.Visibility.ToString());

            // S-17：内容净化 + 长度校验（上限 500 字符，超长拒绝）
            var safeContent = SafeContentSanitizer.Sanitize(command.Content);
            if (safeContent.Length > 500)
                throw new ArgumentException("推文内容不能超过500个字符");

            // S-17：敏感词过滤（拒绝策略，命中即拒绝发布）
            var (isSensitive, matchedWord) = SensitiveWordFilter.ContainsSensitive(safeContent);
            if (isSensitive)
                throw new InvalidOperationException($"推文内容包含敏感内容（{matchedWord}），已拒绝发布");

            // 媒体校验与解析：FileDev 逐个校验归属（防 IDOR），服务端解析元数据填充 TweetMedia
            var media = await ResolveMediaAsync(command.FileIds, command.UserId, cancellationToken);
            var tweet = Tweet.Create(command.UserId, safeContent, media, linkMetadata, null, parsedVisibility);

            // AsDraft=false：创建后立即提交审核（Draft → Pending）。不直接置 Approved，仍须走管理端审核流程。
            if (!command.AsDraft)
                tweet.Publish();

            // 兼容既有 ISensitiveWordFilter 注入：保留日志补充（真实决策已由上方静态过滤器完成）
            try
            {
                var filterResult = await sensitiveWordFilter.FilterAsync(safeContent);
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

            // 图片审核（仅记录日志，使用 FileDev 解析后的媒体 URL）
            if (media is not null && media.Count > 0)
            {
                try
                {
                    var moderationResult =
                        await imageModerationService.ModerateAsync(media.Select(m => m.MediaUrl));
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

    /// <summary>
    /// 解析媒体：FileDev gRPC GetFileInfo 逐个校验存在性与归属（防 IDOR），
    /// 服务端用 FileDev 元数据构建 TweetMedia（MediaUrl/MediaType/FileSize），客户端不再提供任意 URL。
    /// </summary>
    private async Task<List<TweetMedia>?> ResolveMediaAsync(
        IEnumerable<Guid>? fileIds, Guid userId, CancellationToken cancellationToken)
    {
        if (fileIds is null)
            return null;

        var media = new List<TweetMedia>();
        var sortOrder = 0;
        foreach (var fileId in fileIds.Distinct())
        {
            var info = await fileStorage.GetFileInfoAsync(fileId, cancellationToken);
            if (!info.Success || info.FileId == null)
                throw new KeyNotFoundException($"媒体文件不存在：{fileId}");
            if (info.UserId != userId)
                throw new UnauthorizedAccessException($"无权使用媒体文件：{fileId}");

            var mimeType = Message.Web.API.Extensions.MimeTypeMap.FromFileName(info.FileName);
            var tweetMedia = new TweetMedia(info.FileUri!.ToString(), mimeType, sortOrder++);
            tweetMedia.SetFileSize(info.FileSize);
            media.Add(tweetMedia);
        }
        return media;
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

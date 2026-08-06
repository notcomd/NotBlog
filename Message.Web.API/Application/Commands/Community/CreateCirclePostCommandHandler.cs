
namespace Message.Web.API.Application.Commands.Community;
/// <summary>圈子发帖命令处理程序。</summary>
public class CreateCirclePostCommandHandler(
    ICircleRepository circleRepository,
    ITopicRepository topicRepository,
    ITweetRepository tweetRepository,
    IFileStorageGrpcClient fileStorage,
    ILogger<CreateCirclePostCommandHandler> logger) : IRequestHandler<CreateCirclePostCommand, Guid>
{
    public async Task<Guid> Handler(CreateCirclePostCommand command, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("开始圈子发帖，用户: {UserGuid}, 圈子: {CircleGuid}", command.UserId, command.CircleGuid);

            // 1. 成员校验（圈子帖仅成员可发）
            await CommunityAccessGuard.EnsureCircleMemberAsync(circleRepository, command.CircleGuid, command.UserId);

            // 2. 话题校验：全部存在且有效
            var topicGuids = (command.TopicGuids ?? []).Distinct().ToList();
            if (topicGuids.Count > 10)
                throw new ArgumentException("帖子最多关联10个话题", nameof(command.TopicGuids));
            var topics = topicGuids.Count > 0
                ? (await topicRepository.GetExistingActiveAsync(topicGuids)).ToList()
                : [];
            if (topics.Count != topicGuids.Count)
                throw new KeyNotFoundException("部分话题不存在或已停用");

            // 3. 内容净化 + 敏感词过滤（与全局发帖一致）
            var safeContent = SafeContentSanitizer.Sanitize(command.Content);
            if (safeContent.Length > 2000)
                throw new ArgumentException("帖子内容不能超过2000个字符");
            var (isSensitive, matchedWord) = SensitiveWordFilter.ContainsSensitive(safeContent);
            if (isSensitive)
                throw new InvalidOperationException($"帖子内容包含敏感内容（{matchedWord}），已拒绝发布");

            // 4. 媒体解析（FileDev gRPC 校验归属 + 元数据填充）
            var media = await ResolveMediaAsync(command.FileIds, command.UserId, cancellationToken);

            // 5. 创建并直接发布（圈子内免审核）
            var tweet = Tweet.Create(command.UserId, safeContent, media,
                CreateLinkMetadata(command.LinkUrl), null, Visibility.Public,
                command.CircleGuid, topicGuids);
            tweet.PublishInCircle();

            // 6. 话题帖子数 +1
            foreach (var topic in topics)
            {
                topic.IncrementPostCount();
                await topicRepository.UpdateAsync(topic);
            }

            await tweetRepository.AddAsync(tweet);
            await tweetRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            logger.LogInformation("圈子发帖成功: Tweet={TweetGuid}, Circle={CircleGuid}", tweet.TweetGuid, command.CircleGuid);
            return tweet.TweetGuid;
        }
        catch (Exception ex) when (ex is not UnauthorizedAccessException and not KeyNotFoundException
            and not InvalidOperationException and not ArgumentException)
        {
            logger.LogError(ex, "圈子发帖失败，用户: {UserGuid}, 圈子: {CircleGuid}", command.UserId, command.CircleGuid);
            throw;
        }
    }

    /// <summary>媒体解析：FileDev gRPC 逐个校验存在性与归属（防 IDOR），服务端构建 TweetMedia</summary>
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
}

namespace Message.Infrastructure.Provider;

public class TweetProvider : ITweetProvider
{
    private readonly ITweetRepository _tweetRepository;
    private readonly ITweetInteractionRepository _interactionRepository;
    private readonly ICommentRepository _commentRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISensitiveWordFilter _sensitiveWordFilter;
    private readonly IImageModerationService _imageModerationService;
    private readonly ILogger<TweetProvider> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public TweetProvider(
        ITweetRepository tweetRepository,
        ITweetInteractionRepository interactionRepository,
        ICommentRepository commentRepository,
        ICurrentUserService currentUserService,
        ISensitiveWordFilter sensitiveWordFilter,
        IImageModerationService imageModerationService,
        ILogger<TweetProvider> logger,
        IUnitOfWork unitOfWork)
    {
        _tweetRepository = tweetRepository;
        _interactionRepository = interactionRepository;
        _commentRepository = commentRepository;
        _currentUserService = currentUserService;
        _sensitiveWordFilter = sensitiveWordFilter;
        _imageModerationService = imageModerationService;
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    public async Task<Tweet> CreateTweetAsync(Guid authorGuid, string content,
        IEnumerable<string>? mediaUrls = null, string? linkUrl = null,
        IEnumerable<string>? hashtags = null, string? visibility = null)
    {
        try
        {
            _logger.LogInformation("开始创建推文，作者: {AuthorGuid}", authorGuid);

            var linkMetadata = CreateLinkMetadata(linkUrl);
            var parsedVisibility = ParseVisibility(visibility);
            var tweet = Tweet.Create(authorGuid, content, null, linkMetadata, hashtags, parsedVisibility);

            // 敏感词过滤（仅记录日志）
            try
            {
                var filterResult = await _sensitiveWordFilter.FilterAsync(content);
                if (!filterResult.Passed)
                {
                    _logger.LogWarning("推文包含敏感词，命中词: {MatchedWords}",
                        string.Join(", ", filterResult.MatchedWords));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "敏感词过滤失败，继续创建推文");
            }

            // 图片审核（仅记录日志）
            if (mediaUrls is not null && mediaUrls.Any())
            {
                try
                {
                    var moderationResult = await _imageModerationService.ModerateAsync(mediaUrls);
                    if (!moderationResult.Passed)
                    {
                        _logger.LogWarning("图片审核未通过，原因: {Reason}", moderationResult.Reason);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "图片审核失败，继续创建推文");
                }
            }

            await _tweetRepository.AddAsync(tweet);
            await _unitOfWork.SaveEntitiesAsync();

            _logger.LogInformation("推文创建成功，ID: {TweetGuid}", tweet.TweetGuid);
            return tweet;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建推文失败，作者: {AuthorGuid}", authorGuid);
            throw;
        }
    }

    public async Task<Tweet> SaveDraftAsync(Guid authorGuid, string content,
        IEnumerable<string>? mediaUrls = null, string? linkUrl = null,
        IEnumerable<string>? hashtags = null, string? visibility = null)
    {
        try
        {
            _logger.LogInformation("开始保存草稿，作者: {AuthorGuid}", authorGuid);

            var linkMetadata = CreateLinkMetadata(linkUrl);
            var parsedVisibility = ParseVisibility(visibility);
            var tweet = Tweet.Create(authorGuid, content, null, linkMetadata, hashtags, parsedVisibility);

            await _tweetRepository.AddAsync(tweet);
            await _unitOfWork.SaveEntitiesAsync();

            _logger.LogInformation("草稿保存成功，ID: {TweetGuid}", tweet.TweetGuid);
            return tweet;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存草稿失败，作者: {AuthorGuid}", authorGuid);
            throw;
        }
    }

    public async Task<Tweet> PublishDraftAsync(Guid tweetGuid, Guid authorGuid)
    {
        try
        {
            _logger.LogInformation("开始发布草稿，推文: {TweetGuid}", tweetGuid);

            var tweet = await _tweetRepository.GetByIdAsync(tweetGuid);
            if (tweet == null)
                throw new KeyNotFoundException("推文不存在");

            if (tweet.AuthorGuid != authorGuid)
                throw new UnauthorizedAccessException("无权发布此推文");

            tweet.Publish();
            await _tweetRepository.UpdateAsync(tweet);
            await _unitOfWork.SaveEntitiesAsync();

            _logger.LogInformation("草稿发布成功，ID: {TweetGuid}", tweetGuid);
            return tweet;
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not UnauthorizedAccessException)
        {
            _logger.LogError(ex, "发布草稿失败，推文: {TweetGuid}", tweetGuid);
            throw;
        }
    }

    public async Task<Tweet?> GetTweetAsync(Guid tweetGuid)
    {
        try
        {
            var tweet = await _tweetRepository.GetByIdAsync(tweetGuid);

            if (tweet != null && tweet.TweetStatus == TweetStatus.Approved)
            {
                tweet.IncrementViewCount();
                tweet.RecalculateHotScore();
                await _tweetRepository.UpdateAsync(tweet);
                await _unitOfWork.SaveEntitiesAsync();
            }

            return tweet;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取推文失败，ID: {TweetGuid}", tweetGuid);
            throw;
        }
    }

    public async Task<IEnumerable<Tweet>> GetUserTweetsAsync(Guid userGuid, int page = 1, int pageSize = 20)
    {
        try
        {
            var allTweets = await _tweetRepository.GetByAuthorAsync(userGuid, page, pageSize);

            var currentUserId = _currentUserService.IsAuthenticated ? _currentUserService.GetUserId() : Guid.Empty;

            return allTweets.Where(t =>
                t.TweetStatus == TweetStatus.Approved ||
                (currentUserId != Guid.Empty && t.AuthorGuid == currentUserId && t.TweetStatus == TweetStatus.Draft));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户推文列表失败，用户: {UserGuid}", userGuid);
            throw;
        }
    }

    /// <summary>
    /// 获取指定作者的时间线推文列表
    /// </summary>
    /// <param name="userGuid">用户ID</param>
    /// <param name="page">页码</param>
    /// <param name="pageSize">每页数量</param>
    /// <returns>指定作者的时间线推文列表</returns>
    public async Task<IEnumerable<Tweet>> GetTimelineAsync(Guid userGuid, int page = 1, int pageSize = 20)
    {
        try
        {
            // 简化实现：返回指定作者的时间线推文列表
            return await _tweetRepository.GetTimelineAsync(new[] { userGuid }, page, pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取时间线失败，用户: {UserGuid}", userGuid);
            throw;
        }
    }

    /// <summary>
    /// 获取热门推文列表
    /// </summary>
    /// <param name="page">页码</param>
    /// <param name="pageSize">每页数量</param>
    /// <returns>热门推文列表</returns>
    public async Task<IEnumerable<Tweet>> GetTrendingAsync( int page = 1, int pageSize = 20)
    {
        try
        {
            return await _tweetRepository.GetTrendingAsync(page, pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取热门推文失败");
            throw;
        }
    }


  

    public async Task<Tweet> UpdateDraftAsync(Guid tweetGuid, Guid authorGuid, string content,
        IEnumerable<string>? mediaUrls = null, string? linkUrl = null,
        IEnumerable<string>? hashtags = null, string? visibility = null)
    {
        try
        {
            _logger.LogInformation("开始更新草稿，推文: {TweetGuid}", tweetGuid);

            var linkMetadata = CreateLinkMetadata(linkUrl);
            var parsedVisibility = ParseVisibility(visibility);

            var tweet = await _tweetRepository.GetByIdAsync(tweetGuid);
            if (tweet == null)
                throw new KeyNotFoundException("推文不存在");

            if (tweet.AuthorGuid != authorGuid)
                throw new UnauthorizedAccessException("无权修改此推文");

            tweet.UpdateContent(content);
            await _tweetRepository.UpdateAsync(tweet);
            await _unitOfWork.SaveEntitiesAsync();

            _logger.LogInformation("草稿更新成功，ID: {TweetGuid}", tweetGuid);
            return tweet;
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not UnauthorizedAccessException)
        {
            _logger.LogError(ex, "更新草稿失败，推文: {TweetGuid}", tweetGuid);
            throw;
        }
    }

    public async Task DeleteTweetAsync(Guid tweetGuid, Guid authorGuid)
    {
        try
        {
            _logger.LogInformation("开始删除推文，ID: {TweetGuid}", tweetGuid);

            var tweet = await _tweetRepository.GetByIdAsync(tweetGuid);
            if (tweet == null)
                throw new KeyNotFoundException("推文不存在");

            if (tweet.AuthorGuid != authorGuid && !_currentUserService.IsAdmin())
                throw new UnauthorizedAccessException("无权删除此推文");

            await _tweetRepository.DeleteAsync(tweetGuid);
            await _unitOfWork.SaveEntitiesAsync();

            _logger.LogInformation("推文删除成功，ID: {TweetGuid}", tweetGuid);
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not UnauthorizedAccessException)
        {
            _logger.LogError(ex, "删除推文失败，ID: {TweetGuid}", tweetGuid);
            throw;
        }
    }

    public async Task PinTweetAsync(Guid tweetGuid, Guid authorGuid)
    {
        try
        {
            _logger.LogInformation("开始置顶推文，ID: {TweetGuid}", tweetGuid);

            var tweet = await _tweetRepository.GetByIdAsync(tweetGuid);
            if (tweet == null)
                throw new KeyNotFoundException("推文不存在");

            if (tweet.AuthorGuid != authorGuid)
                throw new UnauthorizedAccessException("无权置顶此推文");

            tweet.Pin();
            await _tweetRepository.UpdateAsync(tweet);
            await _unitOfWork.SaveEntitiesAsync();

            _logger.LogInformation("推文置顶成功，ID: {TweetGuid}", tweetGuid);
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not UnauthorizedAccessException)
        {
            _logger.LogError(ex, "置顶推文失败，ID: {TweetGuid}", tweetGuid);
            throw;
        }
    }

    public async Task UnpinTweetAsync(Guid tweetGuid, Guid authorGuid)
    {
        try
        {
            _logger.LogInformation("开始取消置顶推文，ID: {TweetGuid}", tweetGuid);

            var tweet = await _tweetRepository.GetByIdAsync(tweetGuid);
            if (tweet == null)
                throw new KeyNotFoundException("推文不存在");

            if (tweet.AuthorGuid != authorGuid)
                throw new UnauthorizedAccessException("无权取消置顶此推文");

            tweet.Unpin();
            await _tweetRepository.UpdateAsync(tweet);
            await _unitOfWork.SaveEntitiesAsync();

            _logger.LogInformation("取消置顶推文成功，ID: {TweetGuid}", tweetGuid);
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not UnauthorizedAccessException)
        {
            _logger.LogError(ex, "取消置顶推文失败，ID: {TweetGuid}", tweetGuid);
            throw;
        }
    }

    public async Task<Tweet> LikeAsync(Guid tweetGuid, Guid userGuid)
    {
        try
        {
            _logger.LogInformation("用户点赞推文，用户: {UserGuid}, 推文: {TweetGuid}", userGuid, tweetGuid);

            var exists = await _interactionRepository.ExistsAsync(tweetGuid, userGuid, InteractionType.Like);
            if (exists)
                throw new InvalidOperationException("已经点过赞了");

            var tweet = await _tweetRepository.GetByIdAsync(tweetGuid);
            if (tweet == null)
                throw new KeyNotFoundException("推文不存在");

            var interaction = TweetInteraction.Create(tweetGuid, userGuid, InteractionType.Like);
            await _interactionRepository.AddAsync(interaction);
            tweet.AddLike();
            tweet.RecalculateHotScore();
            await _tweetRepository.UpdateAsync(tweet);
            await _unitOfWork.SaveEntitiesAsync();

            _logger.LogInformation("点赞成功，推文: {TweetGuid}", tweetGuid);
            return tweet;
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not InvalidOperationException)
        {
            _logger.LogError(ex, "点赞失败，推文: {TweetGuid}", tweetGuid);
            throw;
        }
    }

    public async Task<Tweet> UnlikeAsync(Guid tweetGuid, Guid userGuid)
    {
        try
        {
            _logger.LogInformation("用户取消点赞推文，用户: {UserGuid}, 推文: {TweetGuid}", userGuid, tweetGuid);

            var interaction = await _interactionRepository.GetAsync(tweetGuid, userGuid, InteractionType.Like);
            if (interaction == null)
                throw new InvalidOperationException("尚未点赞");

            var tweet = await _tweetRepository.GetByIdAsync(tweetGuid);
            if (tweet == null)
                throw new KeyNotFoundException("推文不存在");

            await _interactionRepository.DeleteAsync(tweetGuid, userGuid, InteractionType.Like);
            tweet.RemoveLike();
            tweet.RecalculateHotScore();
            await _tweetRepository.UpdateAsync(tweet);
            await _unitOfWork.SaveEntitiesAsync();

            _logger.LogInformation("取消点赞成功，推文: {TweetGuid}", tweetGuid);
            return tweet;
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not InvalidOperationException)
        {
            _logger.LogError(ex, "取消点赞失败，推文: {TweetGuid}", tweetGuid);
            throw;
        }
    }

    public async Task<Tweet> FavoriteAsync(Guid tweetGuid, Guid userGuid)
    {
        try
        {
            _logger.LogInformation("用户收藏推文，用户: {UserGuid}, 推文: {TweetGuid}", userGuid, tweetGuid);

            var exists = await _interactionRepository.ExistsAsync(tweetGuid, userGuid, InteractionType.Favorite);
            if (exists)
                throw new InvalidOperationException("已经收藏过了");

            var tweet = await _tweetRepository.GetByIdAsync(tweetGuid);
            if (tweet == null)
                throw new KeyNotFoundException("推文不存在");

            var interaction = TweetInteraction.Create(tweetGuid, userGuid, InteractionType.Favorite);
            await _interactionRepository.AddAsync(interaction);
            tweet.AddFavorite();
            tweet.RecalculateHotScore();
            await _tweetRepository.UpdateAsync(tweet);
            await _unitOfWork.SaveEntitiesAsync();

            _logger.LogInformation("收藏成功，推文: {TweetGuid}", tweetGuid);
            return tweet;
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not InvalidOperationException)
        {
            _logger.LogError(ex, "收藏失败，推文: {TweetGuid}", tweetGuid);
            throw;
        }
    }

    public async Task<Tweet> UnfavoriteAsync(Guid tweetGuid, Guid userGuid)
    {
        try
        {
            _logger.LogInformation("用户取消收藏推文，用户: {UserGuid}, 推文: {TweetGuid}", userGuid, tweetGuid);

            var interaction = await _interactionRepository.GetAsync(tweetGuid, userGuid, InteractionType.Favorite);
            if (interaction == null)
                throw new InvalidOperationException("尚未收藏");

            var tweet = await _tweetRepository.GetByIdAsync(tweetGuid);
            if (tweet == null)
                throw new KeyNotFoundException("推文不存在");

            await _interactionRepository.DeleteAsync(tweetGuid, userGuid, InteractionType.Favorite);
            tweet.RemoveFavorite();
            tweet.RecalculateHotScore();
            await _tweetRepository.UpdateAsync(tweet);
            await _unitOfWork.SaveEntitiesAsync();

            _logger.LogInformation("取消收藏成功，推文: {TweetGuid}", tweetGuid);
            return tweet;
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not InvalidOperationException)
        {
            _logger.LogError(ex, "取消收藏失败，推文: {TweetGuid}", tweetGuid);
            throw;
        }
    }

    public async Task<Tweet> ShareAsync(Guid tweetGuid, Guid userGuid)
    {
        try
        {
            _logger.LogInformation("用户转发推文，用户: {UserGuid}, 推文: {TweetGuid}", userGuid, tweetGuid);

            var tweet = await _tweetRepository.GetByIdAsync(tweetGuid);
            if (tweet == null)
                throw new KeyNotFoundException("推文不存在");

            var interaction = TweetInteraction.Create(tweetGuid, userGuid, InteractionType.Share);
            await _interactionRepository.AddAsync(interaction);
            tweet.AddShare();
            tweet.RecalculateHotScore();
            await _tweetRepository.UpdateAsync(tweet);
            await _unitOfWork.SaveEntitiesAsync();

            _logger.LogInformation("转发成功，推文: {TweetGuid}", tweetGuid);
            return tweet;
        }
        catch (Exception ex) when (ex is not KeyNotFoundException)
        {
            _logger.LogError(ex, "转发失败，推文: {TweetGuid}", tweetGuid);
            throw;
        }
    }

    public async Task<Tweet> CoinAsync(Guid tweetGuid, Guid userGuid)
    {
        try
        {
            _logger.LogInformation("用户投币推文，用户: {UserGuid}, 推文: {TweetGuid}", userGuid, tweetGuid);

            var tweet = await _tweetRepository.GetByIdAsync(tweetGuid);
            if (tweet == null)
                throw new KeyNotFoundException("推文不存在");

            var interaction = TweetInteraction.Create(tweetGuid, userGuid, InteractionType.Coin);
            await _interactionRepository.AddAsync(interaction);
            tweet.AddCoin();
            tweet.RecalculateHotScore();
            await _tweetRepository.UpdateAsync(tweet);
            await _unitOfWork.SaveEntitiesAsync();

            _logger.LogInformation("投币成功，推文: {TweetGuid}", tweetGuid);
            return tweet;
        }
        catch (Exception ex) when (ex is not KeyNotFoundException)
        {
            _logger.LogError(ex, "投币失败，推文: {TweetGuid}", tweetGuid);
            throw;
        }
    }

    public async Task RecordViewAsync(Guid tweetGuid, Guid? userGuid, string? viewerIp)
    {
        try
        {
            var tweet = await _tweetRepository.GetByIdAsync(tweetGuid);
            if (tweet == null)
            {
                _logger.LogWarning("记录查看时推文不存在，ID: {TweetGuid}", tweetGuid);
                return;
            }

            tweet.IncrementViewCount();

            if (tweet.ViewCount % 10 == 0)
            {
                tweet.RecalculateHotScore();
            }

            await _tweetRepository.UpdateAsync(tweet);
            await _unitOfWork.SaveEntitiesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "记录查看失败，推文: {TweetGuid}", tweetGuid);
            throw;
        }
    }

    public async Task<bool> GetInteractionStatusAsync(Guid tweetGuid, Guid userId, InteractionType type)
    {
        try
        {
            return await _interactionRepository.ExistsAsync(tweetGuid, userId, type);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取交互状态失败，推文: {TweetGuid}, 用户: {UserId}", tweetGuid, userId);
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

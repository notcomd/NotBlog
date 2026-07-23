namespace Message.Web.API.APIs;

public static class TweetsApi
{
    public static RouteGroupBuilder MapTweetsApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tweets")
            .WithTags("Tweets");

        group.MapPost("/", async (
            [FromBody] CreateTweetRequest request,
            ITweetProvider tweetProvider,
            ICurrentUserService currentUserService,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("TweetsApi");
            try
            {
                var userId = currentUserService.GetUserId();
                var tweet = await tweetProvider.CreateTweetAsync(userId, request.Content, request.MediaUrls,
                    request.LinkUrl, visibility: request.Visibility);

                var dto = MapToDto(tweet);
                return Results.Ok(ApiResponse<TweetDto>.Created(dto, "推文创建成功"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "创建推文失败");
                return Results.Ok(ApiResponse<TweetDto>.Error("创建推文失败"));
            }
        })
        .WithSummary("创建推文")
        .WithDescription("发布一条新推文")
        .Accepts<CreateTweetRequest>("application/json")
        .Produces<ApiResponse<TweetDto>>();

        group.MapPost("/draft", async (
            [FromBody] CreateTweetRequest request,
            ITweetProvider tweetProvider,
            ICurrentUserService currentUserService,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("TweetsApi");
            try
            {
                var userId = currentUserService.GetUserId();
                var tweet = await tweetProvider.SaveDraftAsync(userId, request.Content, request.MediaUrls,
                    request.LinkUrl, visibility: request.Visibility);

                var dto = MapToDto(tweet);
                return Results.Ok(ApiResponse<TweetDto>.Created(dto, "草稿保存成功"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "保存草稿失败");
                return Results.Ok(ApiResponse<TweetDto>.Error("保存草稿失败"));
            }
        })
        .WithSummary("保存草稿")
        .WithDescription("保存一条推文草稿")
        .Accepts<CreateTweetRequest>("application/json")
        .Produces<ApiResponse<TweetDto>>();

        group.MapGet("/{tweetGuid}", async (
            Guid tweetGuid,
            ITweetProvider tweetProvider,
            ICurrentUserService currentUserService,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("TweetsApi");
            try
            {
                var tweet = await tweetProvider.GetTweetAsync(tweetGuid);
                if (tweet == null)
                    return Results.Ok(ApiResponse<TweetDto>.NotFound("推文不存在"));

                var currentUserId = currentUserService.GetUserId();
                var isLiked = await tweetProvider.GetInteractionStatusAsync(tweetGuid, currentUserId, InteractionType.Like);
                var isFavorited = await tweetProvider.GetInteractionStatusAsync(tweetGuid, currentUserId, InteractionType.Favorite);
                var isCoined = await tweetProvider.GetInteractionStatusAsync(tweetGuid, currentUserId, InteractionType.Coin);

                var dto = MapToDto(tweet, isLiked, isFavorited, isCoined);
                return Results.Ok(ApiResponse<TweetDto>.Ok(dto));
            }
            catch (KeyNotFoundException)
            {
                return Results.Ok(ApiResponse<TweetDto>.NotFound("推文不存在"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "获取推文详情失败");
                return Results.Ok(ApiResponse<TweetDto>.Error("获取推文详情失败"));
            }
        })
        .WithSummary("获取推文详情")
        .WithDescription("根据推文ID获取推文详情，包含当前用户的交互状态")
        .Produces<ApiResponse<TweetDto>>();

        group.MapGet("/user/{userGuid}", async (
            Guid userGuid,
            ITweetProvider tweetProvider,
            ILoggerFactory loggerFactory,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20) =>
        {
            var logger = loggerFactory.CreateLogger("TweetsApi");
            try
            {
                var tweets = await tweetProvider.GetUserTweetsAsync(userGuid, page, pageSize);

                var result = new PagedResult<TweetDto>
                {
                    Items = tweets.Select(t => MapToDto(t)).ToList(),
                    Total = tweets.Count(),
                    Page = page,
                    PageSize = pageSize
                };

                return Results.Ok(ApiResponse<PagedResult<TweetDto>>.Ok(result));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "获取用户推文列表失败");
                return Results.Ok(ApiResponse<PagedResult<TweetDto>>.Error("获取用户推文列表失败"));
            }
        })
        .WithSummary("获取用户推文列表")
        .WithDescription("获取指定用户的推文列表，支持分页")
        .Produces<ApiResponse<PagedResult<TweetDto>>>();

        group.MapGet("/timeline", async (
            ITweetProvider tweetProvider,
            ICurrentUserService currentUserService,
            ILoggerFactory loggerFactory,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20) =>
        {
            var logger = loggerFactory.CreateLogger("TweetsApi");
            try
            {
                var userId = currentUserService.GetUserId();
                var tweets = await tweetProvider.GetTimelineAsync(userId, page, pageSize);

                var result = new PagedResult<TweetDto>
                {
                    Items = tweets.Select(t => MapToDto(t)).ToList(),
                    Total = tweets.Count(),
                    Page = page,
                    PageSize = pageSize
                };

                return Results.Ok(ApiResponse<PagedResult<TweetDto>>.Ok(result));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "获取时间线失败");
                return Results.Ok(ApiResponse<PagedResult<TweetDto>>.Error("获取时间线失败"));
            }
        })
        .WithSummary("获取时间线")
        .WithDescription("获取当前用户的时间线推文，支持分页")
        .Produces<ApiResponse<PagedResult<TweetDto>>>();

        group.MapGet("/trending", async (
            ITweetProvider tweetProvider,
            ILoggerFactory loggerFactory,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20) =>
        {
            var logger = loggerFactory.CreateLogger("TweetsApi");
            try
            {
                var tweets = await tweetProvider.GetTrendingAsync(page, pageSize);

                var result = new PagedResult<TweetDto>
                {
                    Items = tweets.Select(t => MapToDto(t)).ToList(),
                    Total = tweets.Count(),
                    Page = page,
                    PageSize = pageSize
                };

                return Results.Ok(ApiResponse<PagedResult<TweetDto>>.Ok(result));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "获取趋势推文失败");
                return Results.Ok(ApiResponse<PagedResult<TweetDto>>.Error("获取趋势推文失败"));
            }
        })
        .WithSummary("获取趋势推文")
        .WithDescription("获取热门趋势推文列表，支持分页")
        .Produces<ApiResponse<PagedResult<TweetDto>>>();

        group.MapPut("/{tweetGuid}", async (
            Guid tweetGuid,
            [FromBody] UpdateTweetRequest request,
            ITweetProvider tweetProvider,
            ICurrentUserService currentUserService,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("TweetsApi");
            try
            {
                var userId = currentUserService.GetUserId();
                var tweet = await tweetProvider.UpdateDraftAsync(tweetGuid, userId, request.Content,
                    request.MediaUrls, request.LinkUrl, visibility: request.Visibility);

                var dto = MapToDto(tweet);
                return Results.Ok(ApiResponse<TweetDto>.Ok(dto, "草稿更新成功"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "更新草稿失败");
                return Results.Ok(ApiResponse<TweetDto>.Error("更新草稿失败"));
            }
        })
        .WithSummary("更新草稿")
        .WithDescription("更新指定的推文草稿")
        .Accepts<UpdateTweetRequest>("application/json")
        .Produces<ApiResponse<TweetDto>>();

        group.MapDelete("/{tweetGuid}", async (
            Guid tweetGuid,
            ITweetProvider tweetProvider,
            ICurrentUserService currentUserService,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("TweetsApi");
            try
            {
                var userId = currentUserService.GetUserId();
                await tweetProvider.DeleteTweetAsync(tweetGuid, userId);
                return Results.Ok(ApiResponse.Ok("推文已删除"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "删除推文失败");
                return Results.Ok(ApiResponse.Error("删除推文失败"));
            }
        })
        .WithSummary("删除推文")
        .WithDescription("删除指定的推文")
        .Produces<ApiResponse>();

        group.MapPost("/{tweetGuid}/pin", async (
            Guid tweetGuid,
            ITweetProvider tweetProvider,
            ICurrentUserService currentUserService,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("TweetsApi");
            try
            {
                var userId = currentUserService.GetUserId();
                await tweetProvider.PinTweetAsync(tweetGuid, userId);
                return Results.Ok(ApiResponse.Ok("推文已置顶"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "置顶推文失败");
                return Results.Ok(ApiResponse.Error("置顶推文失败"));
            }
        })
        .WithSummary("置顶推文")
        .WithDescription("将指定推文置顶")
        .Produces<ApiResponse>();

        group.MapPost("/{tweetGuid}/unpin", async (
            Guid tweetGuid,
            ITweetProvider tweetProvider,
            ICurrentUserService currentUserService,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("TweetsApi");
            try
            {
                var userId = currentUserService.GetUserId();
                await tweetProvider.UnpinTweetAsync(tweetGuid, userId);
                return Results.Ok(ApiResponse.Ok("已取消置顶"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "取消置顶失败");
                return Results.Ok(ApiResponse.Error("取消置顶失败"));
            }
        })
        .WithSummary("取消置顶推文")
        .WithDescription("取消指定推文的置顶状态")
        .Produces<ApiResponse>();

        group.MapPost("/{tweetGuid}/like", async (
            Guid tweetGuid,
            ITweetProvider tweetProvider,
            ICurrentUserService currentUserService,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("TweetsApi");
            try
            {
                var userId = currentUserService.GetUserId();
                var tweet = await tweetProvider.LikeAsync(tweetGuid, userId);
                return Results.Ok(ApiResponse<TweetDto>.Ok(MapToDto(tweet, true)));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "点赞推文失败");
                return Results.Ok(ApiResponse<TweetDto>.Error("点赞推文失败"));
            }
        })
        .WithSummary("点赞推文")
        .WithDescription("对指定推文进行点赞")
        .Produces<ApiResponse<TweetDto>>();

        group.MapDelete("/{tweetGuid}/like", async (
            Guid tweetGuid,
            ITweetProvider tweetProvider,
            ICurrentUserService currentUserService,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("TweetsApi");
            try
            {
                var userId = currentUserService.GetUserId();
                var tweet = await tweetProvider.UnlikeAsync(tweetGuid, userId);
                return Results.Ok(ApiResponse<TweetDto>.Ok(MapToDto(tweet)));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "取消点赞失败");
                return Results.Ok(ApiResponse<TweetDto>.Error("取消点赞失败"));
            }
        })
        .WithSummary("取消点赞")
        .WithDescription("取消对指定推文的点赞")
        .Produces<ApiResponse<TweetDto>>();

        group.MapPost("/{tweetGuid}/favorite", async (
            Guid tweetGuid,
            ITweetProvider tweetProvider,
            ICurrentUserService currentUserService,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("TweetsApi");
            try
            {
                var userId = currentUserService.GetUserId();
                var tweet = await tweetProvider.FavoriteAsync(tweetGuid, userId);
                return Results.Ok(ApiResponse<TweetDto>.Ok(MapToDto(tweet, isFavorited: true)));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "收藏推文失败");
                return Results.Ok(ApiResponse<TweetDto>.Error("收藏推文失败"));
            }
        })
        .WithSummary("收藏推文")
        .WithDescription("收藏指定推文")
        .Produces<ApiResponse<TweetDto>>();

        group.MapDelete("/{tweetGuid}/favorite", async (
            Guid tweetGuid,
            ITweetProvider tweetProvider,
            ICurrentUserService currentUserService,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("TweetsApi");
            try
            {
                var userId = currentUserService.GetUserId();
                var tweet = await tweetProvider.UnfavoriteAsync(tweetGuid, userId);
                return Results.Ok(ApiResponse<TweetDto>.Ok(MapToDto(tweet)));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "取消收藏失败");
                return Results.Ok(ApiResponse<TweetDto>.Error("取消收藏失败"));
            }
        })
        .WithSummary("取消收藏")
        .WithDescription("取消对指定推文的收藏")
        .Produces<ApiResponse<TweetDto>>();

        group.MapPost("/{tweetGuid}/share", async (
            Guid tweetGuid,
            ITweetProvider tweetProvider,
            ICurrentUserService currentUserService,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("TweetsApi");
            try
            {
                var userId = currentUserService.GetUserId();
                var tweet = await tweetProvider.ShareAsync(tweetGuid, userId);
                return Results.Ok(ApiResponse<TweetDto>.Ok(MapToDto(tweet)));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "分享推文失败");
                return Results.Ok(ApiResponse<TweetDto>.Error("分享推文失败"));
            }
        })
        .WithSummary("分享推文")
        .WithDescription("分享指定推文")
        .Produces<ApiResponse<TweetDto>>();

        group.MapPost("/{tweetGuid}/coin", async (
            Guid tweetGuid,
            ITweetProvider tweetProvider,
            ICurrentUserService currentUserService,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("TweetsApi");
            try
            {
                var userId = currentUserService.GetUserId();
                var tweet = await tweetProvider.CoinAsync(tweetGuid, userId);
                return Results.Ok(ApiResponse<TweetDto>.Ok(MapToDto(tweet, isCoined: true)));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "投币失败");
                return Results.Ok(ApiResponse<TweetDto>.Error("投币失败"));
            }
        })
        .WithSummary("投币推文")
        .WithDescription("对指定推文进行投币")
        .Produces<ApiResponse<TweetDto>>();

        group.MapPost("/{tweetGuid}/view", async (
            Guid tweetGuid,
            ITweetProvider tweetProvider,
            ICurrentUserService currentUserService,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("TweetsApi");
            try
            {
                var userId = currentUserService.GetUserId();
                await tweetProvider.RecordViewAsync(tweetGuid, userId, null);
                return Results.Ok(ApiResponse.Ok("已记录查看"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "记录查看失败");
                return Results.Ok(ApiResponse.Error("记录查看失败"));
            }
        })
        .WithSummary("记录查看")
        .WithDescription("记录用户查看了指定推文")
        .Produces<ApiResponse>();

        return group;
    }

    private static TweetDto MapToDto(Tweet tweet, bool isLiked = false, bool isFavorited = false, bool isCoined = false) => new()
    {
        TweetGuid = tweet.TweetGuid,
        Author = new UserBriefDto
        {
            UserGuid = tweet.AuthorGuid,
            UserName = tweet.AuthorGuid.ToString("N")[..8]
        },
        Content = tweet.Content,
        MediaUrls = tweet.Media.Select(m => m.MediaUrl).ToList(),
        LinkMetadata = tweet.LinkMetadata is not null
            ? new LinkMetadataDto
            {
                Url = tweet.LinkMetadata.Url,
                Title = tweet.LinkMetadata.Title,
                Description = tweet.LinkMetadata.Description,
                Image = tweet.LinkMetadata.Image
            }
            : null,
        Hashtags = tweet.Hashtags.Any() ? tweet.Hashtags.ToList() : null,
        TweetStatus = tweet.TweetStatus.ToString(),
        Visibility = tweet.Visibility.ToString(),
        ViewCount = tweet.ViewCount,
        LikeCount = tweet.LikeCount,
        CommentCount = tweet.CommentCount,
        ShareCount = tweet.ShareCount,
        CoinCount = tweet.CoinCount,
        FavoriteCount = tweet.FavoriteCount,
        HotScore = tweet.HotScore,
        PublishTime = tweet.PublishTime,
        CreateTime = tweet.CreateTime,
        IsLiked = isLiked,
        IsFavorited = isFavorited,
        IsCoined = isCoined
    };
}

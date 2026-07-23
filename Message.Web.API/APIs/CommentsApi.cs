namespace Message.Web.API.APIs;

public static class CommentsApi
{
    public static RouteGroupBuilder MapCommentsApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/comments")
            .WithTags("Comments");

        group.MapPost("/", async (
            [FromBody] CreateCommentRequest request,
            ICommentProvider commentProvider,
            ICurrentUserService currentUserService,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("CommentsApi");
            try
            {
                var userId = currentUserService.GetUserId();
                _ = await commentProvider.AddCommentAsync(
                    request.TweetGuid, userId, request.Content,
                    request.ParentGuid, request.ReplyToGuid);

                return Results.Ok(ApiResponse.Ok("评论发布成功"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "发布评论失败");
                return Results.Ok(ApiResponse.Error("发布评论失败"));
            }
        })
        .WithSummary("发布评论")
        .WithDescription("对推文发布评论或回复")
        .Accepts<CreateCommentRequest>("application/json")
        .Produces<ApiResponse>();

        group.MapGet("/tweet/{tweetGuid}", async (
            Guid tweetGuid,
            ICommentProvider commentProvider,
            ILoggerFactory loggerFactory,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20) =>
        {
            var logger = loggerFactory.CreateLogger("CommentsApi");
            try
            {
                var comments = await commentProvider.GetTweetCommentsAsync(tweetGuid, page, pageSize);

                var result = new PagedResult<CommentDto>
                {
                    Items = [.. comments.Select(c => new CommentDto
                    {
                        CommentGuid = c.CommentGuid,
                        TweetGuid = c.TweetGuid,
                        User = new UserBriefDto { UserGuid = c.UserGuid, UserName = string.Empty },
                        ParentGuid = c.ParentGuid,
                        ReplyToGuid = c.ReplyToGuid,
                        Content = c.Content,
                        LikeCount = c.LikeCount,
                        ReplyCount = c.ReplyCount,
                        IsDeleted = c.IsDeleted,
                        CreateTime = c.CreateTime
                    })],
                    Total = comments.Count(),
                    Page = page,
                    PageSize = pageSize
                };

                return Results.Ok(ApiResponse<PagedResult<CommentDto>>.Ok(result));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "获取推文评论失败");
                return Results.Ok(ApiResponse<PagedResult<CommentDto>>.Error("获取推文评论失败"));
            }
        })
        .WithSummary("获取推文评论")
        .WithDescription("获取指定推文的评论列表，支持分页")
        .Produces<ApiResponse<PagedResult<CommentDto>>>();

        group.MapGet("/{commentGuid}/replies", async (
            Guid commentGuid,
            ICommentProvider commentProvider,
            ILoggerFactory loggerFactory,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10) =>
        {
            var logger = loggerFactory.CreateLogger("CommentsApi");
            try
            {
                var replies = await commentProvider.GetCommentRepliesAsync(commentGuid, page, pageSize);

                var result = new PagedResult<CommentDto>
                {
                    Items = [.. replies.Select(c => new CommentDto
                    {
                        CommentGuid = c.CommentGuid,
                        TweetGuid = c.TweetGuid,
                        User = new UserBriefDto { UserGuid = c.UserGuid, UserName = string.Empty },
                        ParentGuid = c.ParentGuid,
                        ReplyToGuid = c.ReplyToGuid,
                        Content = c.Content,
                        LikeCount = c.LikeCount,
                        ReplyCount = c.ReplyCount,
                        IsDeleted = c.IsDeleted,
                        CreateTime = c.CreateTime
                    })],
                    Total = replies.Count(),
                    Page = page,
                    PageSize = pageSize
                };

                return Results.Ok(ApiResponse<PagedResult<CommentDto>>.Ok(result));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "获取评论回复失败");
                return Results.Ok(ApiResponse<PagedResult<CommentDto>>.Error("获取评论回复失败"));
            }
        })
        .WithSummary("获取评论回复")
        .WithDescription("获取指定评论的回复列表，支持分页")
        .Produces<ApiResponse<PagedResult<CommentDto>>>();

        group.MapDelete("/{commentGuid}", async (
            Guid commentGuid,
            ICommentProvider commentProvider,
            ICurrentUserService currentUserService,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("CommentsApi");
            try
            {
                var userId = currentUserService.GetUserId();
                await commentProvider.DeleteCommentAsync(commentGuid, userId);

                return Results.Ok(ApiResponse.Ok("评论已删除"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "删除评论失败");
                return Results.Ok(ApiResponse.Error("删除评论失败"));
            }
        })
        .WithSummary("删除评论")
        .WithDescription("删除指定评论")
        .Produces<ApiResponse>();

        return group;
    }
}

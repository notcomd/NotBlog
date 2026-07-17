using Message.Domain.Entities.Tweet;
using Message.Domain.IProvider;
using Message.Domain.IServices;
using Message.Web.API.Dto;
using Message.Web.API.Dto.Request;
using Message.Web.API.Dto.Response;
using Microsoft.AspNetCore.Mvc;

namespace Message.Web.API.Controllers;

[ApiController]
[Route("api/comments")]
public class CommentsController : ControllerBase
{
    private readonly ICommentProvider _commentProvider;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CommentsController> _logger;

    public CommentsController(
        ICommentProvider commentProvider,
        ICurrentUserService currentUserService,
        ILogger<CommentsController> logger)
    {
        _commentProvider = commentProvider;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse>> AddCommentAsync([FromBody] CreateCommentRequest request)
    {
        var userId = _currentUserService.GetUserId();
        _ = await _commentProvider.AddCommentAsync(
            request.TweetGuid, userId, request.Content,
            request.ParentGuid, request.ReplyToGuid);

        return Ok(ApiResponse.Ok("评论发布成功"));
    }

    [HttpGet("tweet/{tweetGuid}")]
    public async Task<ActionResult<ApiResponse<PagedResult<CommentDto>>>> GetTweetCommentsAsync(
        Guid tweetGuid, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var comments = await _commentProvider.GetTweetCommentsAsync(tweetGuid, page, pageSize);

        var result = new PagedResult<CommentDto>
        {
            Items = [.. comments.Select(MapToDto)],
            Total = comments.Count(),
            Page = page,
            PageSize = pageSize
        };

        return Ok(ApiResponse<PagedResult<CommentDto>>.Ok(result));
    }

    [HttpGet("{commentGuid}/replies")]
    public async Task<ActionResult<ApiResponse<PagedResult<CommentDto>>>> GetCommentRepliesAsync(
        Guid commentGuid, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var replies = await _commentProvider.GetCommentRepliesAsync(commentGuid, page, pageSize);

        var result = new PagedResult<CommentDto>
        {
            Items = [.. replies.Select(MapToDto)],
            Total = replies.Count(),
            Page = page,
            PageSize = pageSize
        };

        return Ok(ApiResponse<PagedResult<CommentDto>>.Ok(result));
    }

    [HttpDelete("{commentGuid}")]
    public async Task<ActionResult<ApiResponse>> DeleteCommentAsync(Guid commentGuid)
    {
        var userId = _currentUserService.GetUserId();
        await _commentProvider.DeleteCommentAsync(commentGuid, userId);
        return Ok(ApiResponse.Ok("评论已删除"));
    }

    private static CommentDto MapToDto(Comment c) => new()
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
    };
}

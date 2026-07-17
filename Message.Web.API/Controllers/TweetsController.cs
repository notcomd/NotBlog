using Message.Domain.Entities.Tweet;
using Message.Domain.Enums;
using Message.Domain.IProvider;
using Message.Domain.IRepository;
using Message.Domain.IServices;
using Message.Web.API.Dto;
using Message.Web.API.Dto.Request;
using Message.Web.API.Dto.Response;
using Microsoft.AspNetCore.Mvc;

namespace Message.Web.API.Controllers;

[ApiController]
[Route("api/tweets")]
public class TweetsController : ControllerBase
{
    private readonly ITweetProvider _tweetProvider;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITweetInteractionRepository _interactionRepository;

    public TweetsController(
        ITweetProvider tweetProvider,
        ICurrentUserService currentUserService,
        ITweetInteractionRepository interactionRepository)
    {
        _tweetProvider = tweetProvider;
        _currentUserService = currentUserService;
        _interactionRepository = interactionRepository;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<TweetDto>>> CreateTweet([FromBody] CreateTweetRequest request)
    {
        var userId = _currentUserService.GetUserId();
        var visibility = ParseVisibility(request.Visibility);
        var linkMetadata = string.IsNullOrWhiteSpace(request.LinkUrl)
            ? null
            : LinkMetadata.Create(request.LinkUrl);

        var tweet = await _tweetProvider.CreateTweetAsync(userId, request.Content, request.MediaUrls,
            linkMetadata, visibility: visibility);

        var dto = MapToDto(tweet);
        return Ok(ApiResponse<TweetDto>.Created(dto, "推文创建成功"));
    }

    [HttpPost("draft")]
    public async Task<ActionResult<ApiResponse<TweetDto>>> SaveDraft([FromBody] CreateTweetRequest request)
    {
        var userId = _currentUserService.GetUserId();
        var visibility = ParseVisibility(request.Visibility);
        var linkMetadata = string.IsNullOrWhiteSpace(request.LinkUrl)
            ? null
            : LinkMetadata.Create(request.LinkUrl);

        var tweet = await _tweetProvider.SaveDraftAsync(userId, request.Content, request.MediaUrls,
            linkMetadata, visibility: visibility);

        var dto = MapToDto(tweet);
        return Ok(ApiResponse<TweetDto>.Created(dto, "草稿保存成功"));
    }

    [HttpGet("{tweetGuid}")]
    public async Task<ActionResult<ApiResponse<TweetDto>>> GetTweet(Guid tweetGuid)
    {
        try
        {
            var tweet = await _tweetProvider.GetTweetAsync(tweetGuid);
            if (tweet == null)
                return NotFound(ApiResponse<TweetDto>.NotFound("推文不存在"));

            var currentUserId = _currentUserService.GetUserId();
            var isLiked = await _interactionRepository.ExistsAsync(tweetGuid, currentUserId, InteractionType.Like);
            var isFavorited = await _interactionRepository.ExistsAsync(tweetGuid, currentUserId, InteractionType.Favorite);
            var isCoined = await _interactionRepository.ExistsAsync(tweetGuid, currentUserId, InteractionType.Coin);

            var dto = MapToDto(tweet, isLiked, isFavorited, isCoined);
            return Ok(ApiResponse<TweetDto>.Ok(dto));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<TweetDto>.NotFound("推文不存在"));
        }
    }

    [HttpGet("user/{userGuid}")]
    public async Task<ActionResult<ApiResponse<PagedResult<TweetDto>>>> GetUserTweets(
        Guid userGuid, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var tweets = await _tweetProvider.GetUserTweetsAsync(userGuid, page, pageSize);

        var result = new PagedResult<TweetDto>
        {
            Items = tweets.Select(t => MapToDto(t)).ToList(),
            TotalCount = tweets.Count(),
            Page = page,
            PageSize = pageSize
        };

        return Ok(ApiResponse<PagedResult<TweetDto>>.Ok(result));
    }

    [HttpGet("timeline")]
    public async Task<ActionResult<ApiResponse<PagedResult<TweetDto>>>> GetTimeline(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = _currentUserService.GetUserId();
        var tweets = await _tweetProvider.GetTimelineAsync(userId, page, pageSize);

        var result = new PagedResult<TweetDto>
        {
            Items = tweets.Select(t => MapToDto(t)).ToList(),
            TotalCount = tweets.Count(),
            Page = page,
            PageSize = pageSize
        };

        return Ok(ApiResponse<PagedResult<TweetDto>>.Ok(result));
    }

    [HttpGet("trending")]
    public async Task<ActionResult<ApiResponse<PagedResult<TweetDto>>>> GetTrending(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var tweets = await _tweetProvider.GetTrendingAsync(page, pageSize);

        var result = new PagedResult<TweetDto>
        {
            Items = tweets.Select(t => MapToDto(t)).ToList(),
            TotalCount = tweets.Count(),
            Page = page,
            PageSize = pageSize
        };

        return Ok(ApiResponse<PagedResult<TweetDto>>.Ok(result));
    }

    [HttpPut("{tweetGuid}")]
    public async Task<ActionResult<ApiResponse<TweetDto>>> UpdateDraft(
        Guid tweetGuid, [FromBody] UpdateTweetRequest request)
    {
        var userId = _currentUserService.GetUserId();
        var linkMetadata = string.IsNullOrWhiteSpace(request.LinkUrl)
            ? null
            : LinkMetadata.Create(request.LinkUrl);
        var visibility = ParseVisibility(request.Visibility);

        var tweet = await _tweetProvider.UpdateDraftAsync(tweetGuid, userId, request.Content,
            request.MediaUrls, linkMetadata, visibility: visibility);

        var dto = MapToDto(tweet);
        return Ok(ApiResponse<TweetDto>.Ok(dto, "草稿更新成功"));
    }

    [HttpDelete("{tweetGuid}")]
    public async Task<ActionResult<ApiResponse>> DeleteTweet(Guid tweetGuid)
    {
        var userId = _currentUserService.GetUserId();
        await _tweetProvider.DeleteTweetAsync(tweetGuid, userId);
        return Ok(ApiResponse.Ok("推文已删除"));
    }

    [HttpPost("{tweetGuid}/pin")]
    public async Task<ActionResult<ApiResponse>> PinTweet(Guid tweetGuid)
    {
        var userId = _currentUserService.GetUserId();
        await _tweetProvider.PinTweetAsync(tweetGuid, userId);
        return Ok(ApiResponse.Ok("推文已置顶"));
    }

    [HttpPost("{tweetGuid}/unpin")]
    public async Task<ActionResult<ApiResponse>> UnpinTweet(Guid tweetGuid)
    {
        var userId = _currentUserService.GetUserId();
        await _tweetProvider.UnpinTweetAsync(tweetGuid, userId);
        return Ok(ApiResponse.Ok("已取消置顶"));
    }

    [HttpPost("{tweetGuid}/like")]
    public async Task<ActionResult<ApiResponse<TweetDto>>> Like(Guid tweetGuid)
    {
        var userId = _currentUserService.GetUserId();
        var tweet = await _tweetProvider.LikeAsync(tweetGuid, userId);
        return Ok(ApiResponse<TweetDto>.Ok(MapToDto(tweet, true)));
    }

    [HttpDelete("{tweetGuid}/like")]
    public async Task<ActionResult<ApiResponse<TweetDto>>> Unlike(Guid tweetGuid)
    {
        var userId = _currentUserService.GetUserId();
        var tweet = await _tweetProvider.UnlikeAsync(tweetGuid, userId);
        return Ok(ApiResponse<TweetDto>.Ok(MapToDto(tweet)));
    }

    [HttpPost("{tweetGuid}/favorite")]
    public async Task<ActionResult<ApiResponse<TweetDto>>> Favorite(Guid tweetGuid)
    {
        var userId = _currentUserService.GetUserId();
        var tweet = await _tweetProvider.FavoriteAsync(tweetGuid, userId);
        return Ok(ApiResponse<TweetDto>.Ok(MapToDto(tweet, isFavorited: true)));
    }

    [HttpDelete("{tweetGuid}/favorite")]
    public async Task<ActionResult<ApiResponse<TweetDto>>> Unfavorite(Guid tweetGuid)
    {
        var userId = _currentUserService.GetUserId();
        var tweet = await _tweetProvider.UnfavoriteAsync(tweetGuid, userId);
        return Ok(ApiResponse<TweetDto>.Ok(MapToDto(tweet)));
    }

    [HttpPost("{tweetGuid}/share")]
    public async Task<ActionResult<ApiResponse<TweetDto>>> Share(Guid tweetGuid)
    {
        var userId = _currentUserService.GetUserId();
        var tweet = await _tweetProvider.ShareAsync(tweetGuid, userId);
        return Ok(ApiResponse<TweetDto>.Ok(MapToDto(tweet)));
    }

    [HttpPost("{tweetGuid}/coin")]
    public async Task<ActionResult<ApiResponse<TweetDto>>> Coin(Guid tweetGuid)
    {
        var userId = _currentUserService.GetUserId();
        var tweet = await _tweetProvider.CoinAsync(tweetGuid, userId);
        return Ok(ApiResponse<TweetDto>.Ok(MapToDto(tweet, isCoined: true)));
    }

    [HttpPost("{tweetGuid}/view")]
    public async Task<ActionResult<ApiResponse>> RecordView(Guid tweetGuid)
    {
        var userId = _currentUserService.GetUserId();
        await _tweetProvider.RecordViewAsync(tweetGuid, userId, null);
        return Ok(ApiResponse.Ok("已记录查看"));
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

    private static Visibility ParseVisibility(string? visibility) =>
        visibility?.ToLower() switch
        {
            "followers" => Visibility.Followers,
            "private" => Visibility.Private,
            _ => Visibility.Public
        };
}


namespace Message.Web.API.Services;

/// <summary>
/// 社区实时推送服务。
/// <para>
/// 统一封装 SignalR 向圈子频道（circle:{id}）及个人推送社区事件的能力，
/// 供 DomainEventHandlers 在领域事件发生后调用（推送失败不影响主流程，由调用方 try-catch 包裹）。
/// </para>
/// </summary>
public class CommunityDeliveryService
{
    private readonly IHubContext<CommunityHub, ICommunityClient> _hubContext;
    private readonly ILogger<CommunityDeliveryService> _logger;

    public CommunityDeliveryService(
        IHubContext<CommunityHub, ICommunityClient> hubContext,
        ILogger<CommunityDeliveryService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    private ICommunityClient Circle(Guid circleGuid) =>
        _hubContext.Clients.Group(CommunityHub.CircleGroupName(circleGuid));

    /// <summary>推送圈子新帖到频道</summary>
    public Task PushPostPublishedAsync(Guid circleGuid, CommunityPostDto post)
    {
        _logger.LogDebug("推送圈子新帖: Circle={CircleGuid}, Tweet={TweetGuid}", circleGuid, post.TweetGuid);
        return Circle(circleGuid).PostPublished(post);
    }

    /// <summary>推送圈子帖新评论到频道</summary>
    public Task PushCommentAddedAsync(Guid circleGuid, Guid tweetGuid, CommentDto comment)
    {
        _logger.LogDebug("推送圈子帖评论: Circle={CircleGuid}, Tweet={TweetGuid}", circleGuid, tweetGuid);
        return Circle(circleGuid).CommentAdded(tweetGuid, comment);
    }

    /// <summary>推送圈子帖点赞到频道</summary>
    public Task PushPostLikedAsync(Guid circleGuid, Guid tweetGuid, Guid userGuid, int likeCount)
    {
        return Circle(circleGuid).PostLiked(tweetGuid, userGuid, likeCount);
    }

    /// <summary>推送圈子帖收藏到频道</summary>
    public Task PushPostFavoritedAsync(Guid circleGuid, Guid tweetGuid, Guid userGuid, int favoriteCount)
    {
        return Circle(circleGuid).PostFavorited(tweetGuid, userGuid, favoriteCount);
    }

    /// <summary>推送新成员加入圈子到频道</summary>
    public Task PushMemberJoinedAsync(Guid circleGuid, Guid userGuid, string role)
    {
        return Circle(circleGuid).MemberJoined(circleGuid, userGuid, role);
    }

    /// <summary>推送成员退出到频道</summary>
    public Task PushMemberLeftAsync(Guid circleGuid, Guid userGuid)
    {
        return Circle(circleGuid).MemberLeft(circleGuid, userGuid);
    }

    /// <summary>推送成员被移出到频道</summary>
    public Task PushMemberRemovedAsync(Guid circleGuid, Guid userGuid)
    {
        return Circle(circleGuid).MemberRemoved(circleGuid, userGuid);
    }

    /// <summary>推送邀请通知给个人（Clients.User 需 IUserIdProvider 支持，已注册 MessageUserIdProvider）</summary>
    public Task PushInvitedAsync(Guid userId, Guid circleGuid, Guid inviteGuid)
    {
        _logger.LogDebug("推送圈子邀请: User={UserGuid}, Circle={CircleGuid}", userId, circleGuid);
        return _hubContext.Clients.User(userId.ToString()).InvitedToCircle(circleGuid, inviteGuid);
    }
}

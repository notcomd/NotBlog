
namespace Message.Web.API.Hubs;

/// <summary>
/// 社区实时事件客户端接口（服务端 → 客户端）。
/// <para>圈子成员通过 CommunityHub.JoinCircle 订阅 circle:{id} 频道后接收以下事件。</para>
/// </summary>
public interface ICommunityClient
{
    /// <summary>圈子新帖发布</summary>
    Task PostPublished(CommunityPostDto post);

    /// <summary>圈子帖收到新评论</summary>
    Task CommentAdded(Guid tweetGuid, CommentDto comment);

    /// <summary>圈子帖被点赞</summary>
    Task PostLiked(Guid tweetGuid, Guid userGuid, int likeCount);

    /// <summary>圈子帖被收藏</summary>
    Task PostFavorited(Guid tweetGuid, Guid userGuid, int favoriteCount);

    /// <summary>新成员加入圈子</summary>
    Task MemberJoined(Guid circleGuid, Guid userGuid, string role);

    /// <summary>成员退出圈子</summary>
    Task MemberLeft(Guid circleGuid, Guid userGuid);

    /// <summary>成员被移出圈子</summary>
    Task MemberRemoved(Guid circleGuid, Guid userGuid);

    /// <summary>被邀请加入圈子（推送给个人）</summary>
    Task InvitedToCircle(Guid circleGuid, Guid inviteGuid);
}

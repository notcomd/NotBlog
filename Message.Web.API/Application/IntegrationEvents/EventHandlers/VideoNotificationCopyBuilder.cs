namespace Message.Web.API.Application.IntegrationEvents.EventHandlers;

/// <summary>
///     Video 服务通知文案构建（事件 → 通知类型 + 标题 + 内容）。
///     <para>昵称经 UserInfo 查询后传入；缺失时回退「一位用户」。</para>
/// </summary>
public static class VideoNotificationCopyBuilder
{
    /// <summary>视频交互事件（点赞/投币）→ 通知内容</summary>
    public static (NotificationType Type, string Title, string Content) Build(
        VideoInteractionMessageIntegrationEvent e, string? actorNickname)
    {
        var nick = Nick(actorNickname);
        return e.InteractionType switch
        {
            VideoInteractionType.VideoLiked => (NotificationType.VideoLiked, "视频被点赞",
                $"{nick} 赞了您的视频《{e.VideoName}》"),
            _ => (NotificationType.VideoCoined, "收到投币",
                $"{nick} 为您的视频《{e.VideoName}》投了币")
        };
    }

    /// <summary>评论发布事件（顶级评论 → 视频新评论；回复 → 评论被回复）→ 通知内容</summary>
    public static (NotificationType Type, string Title, string Content) Build(
        VideoCommentPublishedMessageIntegrationEvent e, string? actorNickname, string? commentPreview)
    {
        var nick = Nick(actorNickname);
        var preview = string.IsNullOrWhiteSpace(commentPreview) ? "" : $"：{commentPreview}";

        return e.RootReviewGuid is null
            ? (NotificationType.VideoCommentAdded, "视频收到新评论",
                $"{nick} 评论了您的视频《{e.VideoName}》{preview}")
            : (NotificationType.VideoCommentReplied, "评论被回复",
                $"{nick} 回复了您在《{e.VideoName}》下的评论{preview}");
    }

    /// <summary>昵称回退：缺失时使用通用称谓</summary>
    private static string Nick(string? nickname)
        => string.IsNullOrWhiteSpace(nickname) ? "一位用户" : nickname;
}
namespace Message.Web.API.Application.IntegrationEvents.EventHandlers;

/// <summary>
///     Markdown 通知文案构建（事件 → 通知类型 + 标题 + 内容）。
///     <para>昵称经 UserInfo 查询后传入；缺失时回退「一位用户」。</para>
/// </summary>
public static class NotificationCopyBuilder
{
    /// <summary>
    ///     交互事件（点赞/投币/评论点赞/评论踩）→ 通知内容
    /// </summary>
    public static (NotificationType Type, string Title, string Content) Build(
        MarkdownInteractionMessageIntegrationEvent e, string? actorNickname)
    {
        var nick = Nick(actorNickname);
        return e.InteractionType switch
        {
            "DocumentLiked" => (NotificationType.MarkdownDocumentLiked, "文章被点赞",
                $"{nick} 赞了您的文章《{e.MarkDownName}》"),
            "DocumentCoined" => (NotificationType.MarkdownDocumentCoined, "收到打赏",
                $"{nick} 打赏了您的文章《{e.MarkDownName}》 {e.Amount} 枚硬币"),
            "ReviewLiked" => (NotificationType.MarkdownReviewLiked, "评论被点赞",
                $"{nick} 赞了您在《{e.MarkDownName}》下的评论"),
            "ReviewDisliked" => (NotificationType.MarkdownReviewDisliked, "评论被点踩",
                $"{nick} 踩了您在《{e.MarkDownName}》下的评论"),
            _ => (NotificationType.MarkdownCommentAdded, "文章互动",
                $"{nick} 与您的文章《{e.MarkDownName}》进行了互动")
        };
    }

    /// <summary>
    ///     评论发布事件（顶级评论 → 文章新评论；回复 → 评论被回复）→ 通知内容
    /// </summary>
    public static (NotificationType Type, string Title, string Content) Build(
        MarkdownCommentPublishedMessageIntegrationEvent e, string? actorNickname, string? commentPreview)
    {
        var nick = Nick(actorNickname);
        var preview = string.IsNullOrWhiteSpace(commentPreview) ? "" : $"：{commentPreview}";

        return e.ParentReviewGuid is null
            ? (NotificationType.MarkdownCommentAdded, "文章收到新评论",
                $"{nick} 评论了您的文章《{e.MarkDownName}》{preview}")
            : (NotificationType.MarkdownCommentReplied, "评论被回复",
                $"{nick} 回复了您在《{e.MarkDownName}》下的评论{preview}");
    }

    /// <summary>昵称回退：缺失时使用通用称谓</summary>
    private static string Nick(string? nickname)
        => string.IsNullOrWhiteSpace(nickname) ? "一位用户" : nickname;
}
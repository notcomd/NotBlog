namespace Message.Domain.Enums;
public enum NotificationType
{
    TweetApproved,
    TweetRejected,
    ReportResolved_Removed,
    ReportResolved_Rejected,
    CommentReplied,
    TweetLiked,
    TweetFavorited,
    TweetCoined,
    TweetShared,
    CircleInvited,
    CircleJoined,
    CirclePostPublished,
    NewFollower,
    GroupDissolved,
    GroupMemberRemoved,
    // ===== 以下为 Markdown 服务作者通知（交互一次化 + 评论发布，v1.1 实施文档） =====
    MarkdownDocumentLiked,
    MarkdownDocumentCoined,
    MarkdownReviewLiked,
    MarkdownReviewDisliked,
    MarkdownCommentAdded,
    MarkdownCommentReplied,
    // ===== 文章发布 → 好友通知（v1.2） =====
    MarkdownPublished
}

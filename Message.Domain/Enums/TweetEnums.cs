namespace Message.Domain.Enums;

public enum TweetStatus
{
    Draft,
    Pending,
    Approved,
    Rejected
}

public enum Visibility
{
    Public,
    Followers,
    Private
}

public enum InteractionType
{
    Like,
    Favorite,
    Share,
    Coin
}

public enum ReportStatus
{
    Pending,
    Reviewing,
    Resolved_Removed,
    Resolved_Rejected
}

public enum ReportCategory
{
    Spam,
    Harassment,
    Violence,
    Porn,
    Other
}

public enum ReportTargetType
{
    Tweet,
    Comment
}

public enum AuditAction
{
    Approve,
    Reject
}

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
    TweetShared
}

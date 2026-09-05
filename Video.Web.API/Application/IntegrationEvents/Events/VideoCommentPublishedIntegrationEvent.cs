using Notcomd.EventBus.Core;

namespace Video.Web.API.Application.IntegrationEvents.Events;

/// <summary>
/// 视频评论发布集成事件 — 顶级评论→通知视频作者；回复评论→通知被回复的评论作者。
/// 评论者 == 被通知者时由消费端跳过（自互动过滤）。
/// </summary>
[EventBusName("VideoCommentPublished")]
public record VideoCommentPublishedIntegrationEvent(
    Guid VideoGuid,
    string VideoName,
    Guid ReviewGuid,
    Guid? RootReviewGuid,
    Guid ActorUserId,
    Guid TargetUserId,
    string CommentPreview,
    DateTimeOffset OccurredAt) : IntegrationEvent;
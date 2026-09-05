using Notcomd.EventBus.Core;

namespace Video.Web.API.Application.IntegrationEvents.Events;

/// <summary>视频交互类型（点赞/投币）</summary>
public enum VideoInteractionType
{
    /// <summary>视频被点赞</summary>
    VideoLiked,

    /// <summary>视频被投币</summary>
    VideoCoined
}

/// <summary>
/// 视频交互集成事件（点赞/投币）— 仅首次发生发布（Redis 去重键判定）。
/// 用于跨服务通知视频作者（Message 服务消费后推送站内通知）。
/// </summary>
[EventBusName("VideoInteraction")]
public record VideoInteractionIntegrationEvent(
    VideoInteractionType InteractionType,
    Guid VideoGuid,
    string VideoName,
    Guid ActorUserId,
    Guid TargetUserId,
    DateTimeOffset OccurredAt) : IntegrationEvent;
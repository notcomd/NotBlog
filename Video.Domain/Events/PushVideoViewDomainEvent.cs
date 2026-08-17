
namespace Video.Domain.Events;

/// <summary>
/// 用户开始观看视频时触发的领域事件
/// </summary>
public record VideoWatchStartedDomainEvent(
    Guid VideoHistoryGuid,
    Guid UserGuid,
    Guid VideoGuid,
    DateTimeOffset StartTime) : INotifications;

/// <summary>
/// 用户完成观看视频时触发的领域事件
/// </summary>
public record VideoWatchCompletedDomainEvent(
    Guid VideoHistoryGuid,
    Guid UserGuid,
    Guid VideoGuid,
    TimeSpan Duration,
    DateTimeOffset CompletedAt) : INotifications;

/// <summary>
/// 用户观看进度更新事件（节流触发，用于分析）
/// </summary>
public record VideoWatchProgressUpdatedDomainEvent(
    Guid VideoHistoryGuid,
    Guid UserGuid,
    Guid VideoGuid,
    double Progress,
    DateTimeOffset UpdatedAt) : INotifications;

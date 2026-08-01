using NotMediator;

namespace Video.Web.API.Application.DomainEvents;

/// <summary>
/// 视频发布领域事件 — 包含视频唯一标识符、标题及封面信息。
/// </summary>
public record VideoPublishedDomainEvent(
    Guid VideoGuid,
    string Title,
    string CoverUrl) : INotifications;

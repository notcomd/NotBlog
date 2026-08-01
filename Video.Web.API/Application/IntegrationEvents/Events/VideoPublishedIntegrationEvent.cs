using Notcomd.EventBus.Core;

namespace Video.Web.API.Application.IntegrationEvents.Events;

/// <summary>
/// 视频发布集成事件 — 用于跨服务通知视频发布。
/// 包含视频唯一标识符(guid)、视频标题(title)及视频封面信息(cover)。
/// </summary>
public record VideoPublishedIntegrationEvent(
    Guid VideoGuid,
    string Title,
    string CoverUrl) : IntegrationEvent;

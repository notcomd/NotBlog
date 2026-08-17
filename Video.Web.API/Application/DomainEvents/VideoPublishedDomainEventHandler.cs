using NotMediator;

namespace Video.Web.API.Application.DomainEvents;

/// <summary>
/// 视频发布领域事件处理器 — 将领域事件转换为集成事件并发布到消息总线。
/// </summary>
public class VideoPublishedDomainEventHandler(
    IEventBus eventBus,
    ILogger<VideoPublishedDomainEventHandler> logger)
    : INotificationHandler<VideoPublishedDomainEvent>
{
    public async Task Handler(VideoPublishedDomainEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Video published: {VideoGuid} Title={Title} Cover={Cover}",
            notification.VideoGuid, notification.Title, notification.CoverUrl);

        var integrationEvent = new VideoPublishedIntegrationEvent(
            notification.VideoGuid, notification.Title, notification.CoverUrl);

        await eventBus.PublishAsync(integrationEvent);

        logger.LogInformation("VideoPublishedIntegrationEvent published for {VideoGuid}", notification.VideoGuid);
    }
}

namespace Markdown.Web.API.Application.IntegrationEvents;

/// <summary>
///     集成事件发布辅助：总线故障（RabbitMQ 抖动/断连）不拖垮业务请求——
///     事件发布失败仅记日志，业务已成功落库（与 Message 服务发布器模式一致）。
///     如需严格投递保证，后续可升级为 Outbox 模式
/// </summary>
public static class EventPublishing
{
    public static async Task PublishSafelyAsync(IEventBus eventBus, IntegrationEvent @event, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(eventBus);
        ArgumentNullException.ThrowIfNull(@event);
        ArgumentNullException.ThrowIfNull(logger);

        try
        {
            await eventBus.PublishAsync(@event);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "发布集成事件 {EventType} 失败，业务已成功，该事件可能丢失", @event.GetType().Name);
        }
    }
}

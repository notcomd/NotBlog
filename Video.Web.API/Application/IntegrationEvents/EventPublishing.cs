using Notcomd.EventBus.Core;

namespace Video.Web.API.Application.IntegrationEvents;

/// <summary>
/// 集成事件安全发布（尽力而为投递）：
/// 总线故障仅记录日志，不抛出异常，避免拖垮业务主流程。
/// 与 Markdown 服务的 EventPublishing.PublishSafelyAsync 行为保持一致。
/// </summary>
public static class EventPublishing
{
    public static async Task PublishSafelyAsync(IEventBus bus, IntegrationEvent @event, ILogger? logger = null)
    {
        try
        {
            await bus.PublishAsync(@event);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Event publish failed: {Event}", @event.GetType().Name);
        }
    }
}
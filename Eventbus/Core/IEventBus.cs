namespace Notcomd.EventBus.Core;

/// <summary>
/// 集成事件总线接口（发布-订阅模式）
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// 发布集成事件到消息队列
    /// </summary>
    Task PublishAsync(IntegrationEvent @event);
}

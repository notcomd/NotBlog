namespace Notcomd.EventBus.Core;

/// <summary>
/// JSON 反序列化集成事件处理器基类（兼容旧 API）
/// 自动处理 JSON 反序列化，子类只需实现 Handler(TEvent) 方法。
/// </summary>
public abstract class JsonIntegrationEventHandler<TEvent> : IIntegrationEventHandler<TEvent>
    where TEvent : IntegrationEvent
{
    public abstract Task Handler(TEvent @event);
}

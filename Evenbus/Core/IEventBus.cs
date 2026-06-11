namespace Notcomd.Evenbus;

public interface IEventBus
{
    Task Publish(string eventName, object? eventData);
    Task Subscribe(string eventName, Type handlerType);
    Task Unsubscribe(string eventName, Type handlerType);
}
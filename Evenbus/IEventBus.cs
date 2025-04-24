namespace Notcomd.Evenbus;

public interface IEventBus
{
    /// <summary>
    ///     发布事件
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="eventData"></param>
    void Publish(string eventName, object? eventData);

    /// <summary>
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="handlerType"></param>
    void Subscribe(string eventName, Type handlerType);

    /// <summary>
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="handlerType"></param>
    void Unsubscribe(string eventName, Type handlerType);
}
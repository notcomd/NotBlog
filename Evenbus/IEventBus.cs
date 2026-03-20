namespace Notcomd.Evenbus;

public interface IEventBus
{
    /// <summary>
    /// 发布事件
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="eventData"></param>
    Task Publish(string eventName, object? eventData);

    /// <summary>
    /// 订阅事件
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="handlerType"></param>
    Task Subscribe(string eventName, Type handlerType);

    /// <summary>
    ///  取消订阅
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="handlerType"></param>
    Task Unsubscribe(string eventName, Type handlerType);
}
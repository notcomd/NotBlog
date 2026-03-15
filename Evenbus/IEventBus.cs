namespace Notcomd.Evenbus;

public interface IEventBus
{
    /// <summary>
    /// 发布事件
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="eventData"></param>
    void Publish(string eventName, object? eventData);

    /// <summary>
    /// 订阅事件
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="handlerType"></param>
    void Subscribe(string eventName, Type handlerType);

    /// <summary>
    ///  取消订阅
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="handlerType"></param>
    void Unsubscribe(string eventName, Type handlerType);
}
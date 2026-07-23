namespace Notcomd.EventBus.Core;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class EventBusNameAttribute : Attribute
{
    public EventBusNameAttribute(string eventBusName) => EventName = eventBusName ?? throw new ArgumentNullException(nameof(eventBusName));

    public string EventName { get; }
}
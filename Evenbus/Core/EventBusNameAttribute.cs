namespace Notcomd.Evenbus;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class EventBusNameAttribute : Attribute
{
    public EventBusNameAttribute(string eventBusName) => EventName = eventBusName ?? throw new ArgumentNullException(nameof(eventBusName));

    public string EventName { get; }
}
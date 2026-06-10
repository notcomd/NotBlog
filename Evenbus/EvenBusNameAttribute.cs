namespace Notcomd.Evenbus;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class EvenBusNameAttribute : Attribute
{
    public EvenBusNameAttribute(string eventBusName)
    {
        EventName = eventBusName ?? throw new ArgumentNullException(nameof(eventBusName));
    }

    public string EventName { get; }
}